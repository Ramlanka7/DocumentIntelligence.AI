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

    private IChatService CreateSut() =>
        new ChatService(
            _providerMock.Object,
            _searchMock.Object,
            _uowMock.Object,
            _userMock.Object,
            NullLogger<ChatService>.Instance);

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
    public async Task AskAsync_WhenProviderFails_ReturnsError()
    {
        var request = new ChatRequest(Guid.NewGuid(), [Guid.NewGuid()], "What is section 3?", []);

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
        var docId = Guid.NewGuid();
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

        _userMock.Setup(u => u.UserId).Returns((Guid?)null);

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
    public async Task AskAsync_WithUserId_AttemptsUsageTracking()
    {
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
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

        _userMock.Setup(u => u.UserId).Returns(userId);

        // UnitOfWork is a stub — Repository() will throw; usage tracking must be swallowed.
        _uowMock
            .Setup(u => u.Repository<Domain.Entities.AiUsageMetric>())
            .Throws<NotImplementedException>();

        // Should not throw even though usage tracking fails
        var result = await CreateSut().AskAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Answer.Should().Be("Risks found.");
    }
}
