using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.AI;

public sealed class ChatServiceTests
{
    private readonly Mock<IAIProvider> _providerMock = new();
    private readonly Mock<ISearchService> _searchMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private IChatService CreateSut() =>
        new ChatService(
            _providerMock.Object,
            _searchMock.Object,
            _uowMock.Object,
            _userMock.Object,
            NullLogger<ChatService>.Instance);

    /// <summary>Authenticates the default user and registers one owned document.</summary>
    private Guid ArrangeOwnedDocument()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        var document = AiTestAuth.NewDocumentOwnedBy(_userId);
        AiTestAuth.SetupDocuments(_uowMock, document);
        return document.Id;
    }

    [Fact]
    public async Task AskAsync_WithEmptyMessage_ReturnsFailure()
    {
        var request = new ChatRequest(Guid.NewGuid(), [Guid.NewGuid()], "  ", []);

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.EmptyMessage");
    }

    [Fact]
    public async Task AskAsync_WithNoDocuments_ReturnsFailure()
    {
        var request = new ChatRequest(Guid.NewGuid(), [], "What are the terms?", []);

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.NoDocuments");
    }

    [Fact]
    public async Task AskAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var request = new ChatRequest(Guid.NewGuid(), [Guid.NewGuid()], "What is section 3?", []);

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NotAuthenticated");
    }

    [Fact]
    public async Task AskAsync_WithForeignDocument_ReturnsAccessDenied()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        var foreignDocument = AiTestAuth.NewDocumentOwnedBy(Guid.NewGuid());
        AiTestAuth.SetupDocuments(_uowMock, foreignDocument);

        var request = new ChatRequest(Guid.NewGuid(), [foreignDocument.Id], "What is section 3?", []);

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.AccessDenied");
    }

    [Fact]
    public async Task AskAsync_WithUnknownDocument_ReturnsNotFound()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        AiTestAuth.SetupDocuments(_uowMock);

        var request = new ChatRequest(Guid.NewGuid(), [Guid.NewGuid()], "What is section 3?", []);

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task AskAsync_AsAdmin_AllowsForeignDocument()
    {
        AiTestAuth.Authenticate(_userMock, _userId, "Admin");
        var foreignDocument = AiTestAuth.NewDocumentOwnedBy(Guid.NewGuid());
        AiTestAuth.SetupDocuments(_uowMock, foreignDocument);

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = """{"answer": "Allowed.", "citations": []}""";
        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json, new TokenUsage(10, 5, 0m), "gpt-4o")));

        var request = new ChatRequest(Guid.NewGuid(), [foreignDocument.Id], "Summarize.", []);

        var result = await CreateSut().AskAsync(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AskAsync_WhenProviderFails_ReturnsError()
    {
        var docId = ArrangeOwnedDocument();
        var request = new ChatRequest(Guid.NewGuid(), [docId], "What is section 3?", []);

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AiCompletionResult>(
                Error.Failure("provider.down", "unavailable")));

        var result = await CreateSut().AskAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("provider.down");
    }

    [Fact]
    public async Task AskAsync_WithValidJsonResponse_ReturnsMappedAnswer()
    {
        var docId = ArrangeOwnedDocument();
        var sessionId = Guid.NewGuid();
        var request = new ChatRequest(sessionId, [docId], "What are the payment terms?", []);

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = $$"""
            {
              "answer": "Payment is due within 30 days.",
              "citations": [
                {
                  "documentId": "{{docId}}",
                  "documentName": "contract.pdf",
                  "pageNumber": 5,
                  "paragraphReference": "¶12",
                  "snippet": "Payment is due within 30 days of invoice.",
                  "confidenceScore": 0.95
                }
              ]
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json,
                new TokenUsage(90, 45, 0.001m),
                "gpt-4o")));

        var result = await CreateSut().AskAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Answer.Should().Be("Payment is due within 30 days.");
        result.Value.Citations.Should().HaveCount(1);
        result.Value.Citations[0].DocumentId.Should().Be(docId);
        result.Value.Citations[0].PageNumber.Should().Be(5);
        result.Value.Citations[0].ConfidenceScore.Should().Be(0.95);
        result.Value.Usage.PromptTokens.Should().Be(90);
    }

    [Fact]
    public async Task AskAsync_WhenSessionPersistenceThrows_StillReturnsAnswer()
    {
        var docId = ArrangeOwnedDocument();
        var sessionId = Guid.NewGuid();
        var request = new ChatRequest(sessionId, [docId], "Summarize risks.", []);

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = """{"answer": "Risks found.", "citations": []}""";

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json,
                new TokenUsage(50, 20, 0m),
                "gpt-4o")));

        // Usage-metric persistence failures must be swallowed — answer still returned.
        _uowMock
            .Setup(u => u.Repository<Domain.Entities.AiUsageMetric>())
            .Throws<NotImplementedException>();

        var result = await CreateSut().AskAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Answer.Should().Be("Risks found.");
    }
}
