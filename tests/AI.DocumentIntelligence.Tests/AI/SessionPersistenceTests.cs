using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Infrastructure.AI.Services;
using AI.DocumentIntelligence.Tests.Integration.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.AI;

/// <summary>
/// Proves that a session row and a usage metric (with sessionId) are persisted for
/// analysis, comparison, and chat when a valid user id is present.
/// </summary>
public sealed class SessionPersistenceTests
{
    private readonly Mock<IAIProvider> _providerMock = new();
    private readonly Mock<ISearchService> _searchMock = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public SessionPersistenceTests()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));
    }

    /// <summary>Seeds a document owned by the test user so the ownership check passes.</summary>
    private Guid SeedOwnedDocument(InMemoryUnitOfWork uow)
    {
        var document = AiTestAuth.NewDocumentOwnedBy(_userId);
        ((IUnitOfWork)uow).Repository<Document>().AddAsync(document).GetAwaiter().GetResult();
        return document.Id;
    }

    // ---- Analysis ----

    [Fact]
    public async Task AnalyzeAsync_WithUserId_PersistsAnalysisSessionAndUsageMetric()
    {
        var uow = new InMemoryUnitOfWork();
        var docId = SeedOwnedDocument(uow);

        var sut = new AnalysisService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<AnalysisService>.Instance);

        var json = $$"""
            {
              "executiveSummary": "Test summary",
              "keyFindings": [{"title": "Finding 1", "detail": "detail", "citations": []}],
              "risks": [],
              "recommendations": [],
              "actionItems": [],
              "sources": [
                {
                  "documentId": "{{docId}}",
                  "documentName": "doc.pdf",
                  "pageNumber": 1,
                  "paragraphReference": "¶1",
                  "snippet": "excerpt",
                  "confidenceScore": 0.9
                }
              ]
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(json, new TokenUsage(100, 50, 0.001m), "gpt-4o")));

        var result = await sut.AnalyzeAsync(new AnalysisRequest([docId], "ExecutiveSummary"));

        result.IsSuccess.Should().BeTrue();

        // Session persisted
        var sessions = uow.GetRepository<AnalysisSession>().All;
        sessions.Should().HaveCount(1);
        sessions[0].OwnerId.Should().Be(_userId);
        sessions[0].Status.Should().Be(SessionStatus.Completed);
        sessions[0].ExecutiveSummary.Should().Be("Test summary");
        sessions[0].ReferencedSources.Should().HaveCount(1);
        sessions[0].ReferencedSources.First().DocumentId.Should().Be(docId);
        sessions[0].ReferencedSources.First().ConfidenceScore.Should().Be(0.9);

        // Usage metric persisted with session id
        var metrics = uow.GetRepository<AiUsageMetric>().All;
        metrics.Should().HaveCount(1);
        metrics[0].UserId.Should().Be(_userId);
        metrics[0].SessionId.Should().Be(sessions[0].Id);
        metrics[0].OperationType.Should().Be("Analysis");
        metrics[0].TokenUsage.PromptTokens.Should().Be(100);
        metrics[0].TokenUsage.CompletionTokens.Should().Be(50);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenProviderFails_MarksSessionFailedButStillReturnsError()
    {
        var uow = new InMemoryUnitOfWork();
        var docId = SeedOwnedDocument(uow);

        var sut = new AnalysisService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<AnalysisService>.Instance);

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AiCompletionResult>(Error.Failure("provider.error", "down")));

        var result = await sut.AnalyzeAsync(new AnalysisRequest([docId], "ExecutiveSummary"));

        result.IsFailure.Should().BeTrue();

        // Session should exist but be marked Failed
        var sessions = uow.GetRepository<AnalysisSession>().All;
        sessions.Should().HaveCount(1);
        sessions[0].Status.Should().Be(SessionStatus.Failed);
    }

    // ---- Comparison ----

    [Fact]
    public async Task CompareAsync_WithUserId_PersistsComparisonSessionAndUsageMetric()
    {
        var uow = new InMemoryUnitOfWork();
        var docId1 = SeedOwnedDocument(uow);
        var docId2 = SeedOwnedDocument(uow);

        var sut = new ComparisonService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<ComparisonService>.Instance);

        var json = $$"""
            {
              "executiveOverview": "Comparison overview",
              "differences": [
                {
                  "type": "Modified",
                  "section": "Section 1",
                  "before": "old",
                  "after": "new",
                  "summary": "Content changed",
                  "citations": []
                }
              ],
              "risks": [{"title": "Risk 1", "description": "desc", "severity": "High", "citations": []}],
              "recommendations": [],
              "sources": [
                {
                  "documentId": "{{docId1}}",
                  "documentName": "doc1.pdf",
                  "pageNumber": 2,
                  "paragraphReference": "¶2",
                  "snippet": "text",
                  "confidenceScore": 0.85
                }
              ]
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(json, new TokenUsage(120, 60, 0.002m), "gpt-4o")));

        var result = await sut.CompareAsync(new ComparisonRequest([docId1, docId2], "SideBySide"));

        result.IsSuccess.Should().BeTrue();

        // Session persisted
        var sessions = uow.GetRepository<ComparisonSession>().All;
        sessions.Should().HaveCount(1);
        sessions[0].OwnerId.Should().Be(_userId);
        sessions[0].Status.Should().Be(SessionStatus.Completed);
        sessions[0].ExecutiveOverview.Should().Be("Comparison overview");
        sessions[0].SourceCitations.Should().HaveCount(1);
        sessions[0].SourceCitations.First().DocumentId.Should().Be(docId1);
        sessions[0].SourceCitations.First().ConfidenceScore.Should().BeApproximately(0.85, 0.001);
        sessions[0].DetailedChangeLog.Should().HaveCount(1);
        sessions[0].DetailedChangeLog.First().Status.Should().Be(ChangeStatus.Modified);

        // Usage metric persisted with session id
        var metrics = uow.GetRepository<AiUsageMetric>().All;
        metrics.Should().HaveCount(1);
        metrics[0].UserId.Should().Be(_userId);
        metrics[0].SessionId.Should().Be(sessions[0].Id);
        metrics[0].OperationType.Should().Be("Comparison");
        metrics[0].TokenUsage.PromptTokens.Should().Be(120);
    }

    [Fact]
    public async Task CompareAsync_WhenProviderFails_MarksSessionFailedButStillReturnsError()
    {
        var uow = new InMemoryUnitOfWork();
        var docId1 = SeedOwnedDocument(uow);
        var docId2 = SeedOwnedDocument(uow);

        var sut = new ComparisonService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<ComparisonService>.Instance);

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AiCompletionResult>(Error.Failure("provider.error", "down")));

        var result = await sut.CompareAsync(new ComparisonRequest([docId1, docId2], "Contract"));

        result.IsFailure.Should().BeTrue();

        var sessions = uow.GetRepository<ComparisonSession>().All;
        sessions.Should().HaveCount(1);
        sessions[0].Status.Should().Be(SessionStatus.Failed);
    }

    // ---- Chat ----

    [Fact]
    public async Task AskAsync_WithUserId_CreatesNewChatSessionAndPersistsMessagesAndUsageMetric()
    {
        var uow = new InMemoryUnitOfWork();
        var docId = SeedOwnedDocument(uow);

        var sut = new ChatService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<ChatService>.Instance);

        var json = $$"""
            {
              "answer": "The payment terms are Net 30.",
              "citations": [
                {
                  "documentId": "{{docId}}",
                  "documentName": "contract.pdf",
                  "pageNumber": 5,
                  "paragraphReference": "¶12",
                  "snippet": "Payment within 30 days.",
                  "confidenceScore": 0.95
                }
              ]
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(json, new TokenUsage(90, 45, 0.001m), "gpt-4o")));

        // Pass Guid.Empty as SessionId to force new session creation
        var request = new ChatRequest(Guid.Empty, [docId], "What are the payment terms?", []);
        var result = await sut.AskAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Answer.Should().Be("The payment terms are Net 30.");

        // Session persisted with two messages (user + assistant)
        var sessions = uow.GetRepository<ChatSession>().All;
        sessions.Should().HaveCount(1);
        sessions[0].OwnerId.Should().Be(_userId);
        sessions[0].Status.Should().Be(SessionStatus.InProgress);
        sessions[0].Messages.Should().HaveCount(2);

        var userMsg = sessions[0].Messages.First(m => m.Role == ChatRole.User);
        userMsg.Content.Should().Be("What are the payment terms?");

        var assistantMsg = sessions[0].Messages.First(m => m.Role == ChatRole.Assistant);
        assistantMsg.Content.Should().Be("The payment terms are Net 30.");
        assistantMsg.Citations.Should().HaveCount(1);
        assistantMsg.Citations.First().DocumentId.Should().Be(docId);
        assistantMsg.Citations.First().ConfidenceScore.Should().Be(0.95);
        assistantMsg.TokenUsage.PromptTokens.Should().Be(90);
        assistantMsg.TokenUsage.CompletionTokens.Should().Be(45);

        // Usage metric persisted with session id
        var metrics = uow.GetRepository<AiUsageMetric>().All;
        metrics.Should().HaveCount(1);
        metrics[0].UserId.Should().Be(_userId);
        metrics[0].SessionId.Should().Be(sessions[0].Id);
        metrics[0].OperationType.Should().Be("Chat");
        metrics[0].TokenUsage.PromptTokens.Should().Be(90);
    }

    [Fact]
    public async Task AskAsync_WithExistingSessionId_AppendsMessagesToExistingSession()
    {
        var uow = new InMemoryUnitOfWork();
        var docId = SeedOwnedDocument(uow);

        var sut = new ChatService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<ChatService>.Instance);

        var json = """{"answer": "Second answer.", "citations": []}""";

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(json, new TokenUsage(50, 25, 0m), "gpt-4o")));

        // First turn — creates a new session
        var firstRequest = new ChatRequest(Guid.Empty, [docId], "First question?", []);
        var firstResult = await sut.AskAsync(firstRequest);
        firstResult.IsSuccess.Should().BeTrue();

        var sessions = uow.GetRepository<ChatSession>().All;
        sessions.Should().HaveCount(1);
        var existingSessionId = sessions[0].Id;
        sessions[0].Messages.Should().HaveCount(2); // user + assistant

        // Second turn — append to existing session
        var secondRequest = new ChatRequest(existingSessionId, [docId], "Follow-up question?", []);
        var secondResult = await sut.AskAsync(secondRequest);
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Answer.Should().Be("Second answer.");

        // Still one session, now with 4 messages (2 turns × 2 messages)
        var allSessions = uow.GetRepository<ChatSession>().All;
        allSessions.Should().HaveCount(1);
        allSessions[0].Messages.Should().HaveCount(4);
    }

    [Fact]
    public async Task AskAsync_WithoutUserId_IsRejectedAndPersistsNothing()
    {
        var uow = new InMemoryUnitOfWork();
        var docId = SeedOwnedDocument(uow);
        _userMock.Setup(u => u.IsAuthenticated).Returns(false);
        _userMock.Setup(u => u.UserId).Returns((Guid?)null);

        var sut = new ChatService(
            _providerMock.Object,
            _searchMock.Object,
            uow,
            _userMock.Object,
            NullLogger<ChatService>.Instance);

        var request = new ChatRequest(Guid.NewGuid(), [docId], "Anonymous question?", []);
        var result = await sut.AskAsync(request);

        // Unauthenticated callers are rejected before any AI call or persistence.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NotAuthenticated");
        uow.GetRepository<ChatSession>().All.Should().BeEmpty();
        uow.GetRepository<AiUsageMetric>().All.Should().BeEmpty();
    }
}
