using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.AI;

public sealed class AnalysisServiceTests
{
    private readonly Mock<IAIProvider> _providerMock = new();
    private readonly Mock<ISearchService> _searchMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _userMock = new();

    private IAnalysisService CreateSut() =>
        new AnalysisService(
            _providerMock.Object,
            _searchMock.Object,
            _uowMock.Object,
            _userMock.Object,
            NullLogger<AnalysisService>.Instance);

    [Fact]
    public async Task AnalyzeAsync_WithNoDocuments_ReturnsFailure()
    {
        var request = new AnalysisRequest([], "ExecutiveSummary");

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.NoDocuments");
    }

    [Fact]
    public async Task AnalyzeAsync_WithTooManyDocuments_ReturnsFailure()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var request = new AnalysisRequest(ids, "ExecutiveSummary");

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TooManyDocuments");
    }

    [Fact]
    public async Task AnalyzeAsync_WhenProviderFails_ReturnsProviderError()
    {
        var docId = Guid.NewGuid();
        var request = new AnalysisRequest([docId], "ExecutiveSummary");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AiCompletionResult>(
                Error.Failure("provider.error", "provider down")));

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("provider.error");
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidJsonResponse_ReturnsMappedResult()
    {
        var docId = Guid.NewGuid();
        var request = new AnalysisRequest([docId], "ExecutiveSummary");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = $$"""
            {
              "executiveSummary": "Summary text",
              "keyFindings": [],
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
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json,
                new TokenUsage(100, 50, 0.001m),
                "gpt-4o")));

        _userMock.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().Be("Summary text");
        result.Value.Sources.Should().HaveCount(1);
        result.Value.Sources[0].DocumentId.Should().Be(docId);
        result.Value.Sources[0].ConfidenceScore.Should().Be(0.9);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenJsonIsInvalid_ReturnsParseFailure()
    {
        var docId = Guid.NewGuid();
        var request = new AnalysisRequest([docId], "ExecutiveSummary");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                "not valid json {{",
                new TokenUsage(10, 5, 0m),
                "gpt-4o")));

        _userMock.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.JsonParseFailed");
    }

    [Fact]
    public async Task AnalyzeAsync_WhenMarkdownWrappedJson_ParsesCorrectly()
    {
        var docId = Guid.NewGuid();
        var request = new AnalysisRequest([docId], "KeyInsights");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = $$"""
            ```json
            {
              "executiveSummary": "Wrapped summary",
              "keyFindings": [],
              "risks": [],
              "recommendations": [],
              "actionItems": [],
              "sources": [
                {
                  "documentId": "{{docId}}",
                  "documentName": "doc.pdf",
                  "pageNumber": 2,
                  "paragraphReference": "¶2",
                  "snippet": "text",
                  "confidenceScore": 0.8
                }
              ]
            }
            ```
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json,
                new TokenUsage(80, 40, 0m),
                "gpt-4o")));

        _userMock.Setup(u => u.UserId).Returns((Guid?)null);

        var result = await CreateSut().AnalyzeAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().Be("Wrapped summary");
    }
}
