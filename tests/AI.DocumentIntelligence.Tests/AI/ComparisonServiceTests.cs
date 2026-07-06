using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AI.DocumentIntelligence.Tests.AI;

public sealed class ComparisonServiceTests
{
    private readonly Mock<IAIProvider> _providerMock = new();
    private readonly Mock<ISearchService> _searchMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private IComparisonService CreateSut() =>
        new ComparisonService(
            _providerMock.Object,
            _searchMock.Object,
            _uowMock.Object,
            _userMock.Object,
            NullLogger<ComparisonService>.Instance);

    /// <summary>Authenticates the default user and registers two owned documents.</summary>
    private (Guid First, Guid Second) ArrangeTwoOwnedDocuments()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        var first = AiTestAuth.NewDocumentOwnedBy(_userId);
        var second = AiTestAuth.NewDocumentOwnedBy(_userId);
        AiTestAuth.SetupDocuments(_uowMock, first, second);
        return (first.Id, second.Id);
    }

    [Fact]
    public async Task CompareAsync_WithOneDocument_ReturnsFailure()
    {
        var request = new ComparisonRequest([Guid.NewGuid()], "SideBySide");

        var result = await CreateSut().CompareAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comparison.InsufficientDocuments");
    }

    [Fact]
    public async Task CompareAsync_WithFiveDocuments_ReturnsFailure()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var request = new ComparisonRequest(ids, "Contract");

        var result = await CreateSut().CompareAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comparison.InsufficientDocuments");
    }

    [Fact]
    public async Task CompareAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var request = new ComparisonRequest([Guid.NewGuid(), Guid.NewGuid()], "SideBySide");

        var result = await CreateSut().CompareAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NotAuthenticated");
    }

    [Fact]
    public async Task CompareAsync_WithForeignDocument_ReturnsAccessDenied()
    {
        AiTestAuth.Authenticate(_userMock, _userId);
        var owned = AiTestAuth.NewDocumentOwnedBy(_userId);
        var foreign = AiTestAuth.NewDocumentOwnedBy(Guid.NewGuid());
        AiTestAuth.SetupDocuments(_uowMock, owned, foreign);

        var request = new ComparisonRequest([owned.Id, foreign.Id], "SideBySide");

        var result = await CreateSut().CompareAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.AccessDenied");
    }

    [Fact]
    public async Task CompareAsync_WithValidJsonResponse_ReturnsMappedResult()
    {
        var (docId1, docId2) = ArrangeTwoOwnedDocuments();
        var request = new ComparisonRequest([docId1, docId2], "SideBySide");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = $$"""
            {
              "executiveOverview": "Two documents compared",
              "differences": [
                {
                  "type": "Modified",
                  "section": "Section 1",
                  "before": "old text",
                  "after": "new text",
                  "summary": "Content changed",
                  "citations": [
                    {
                      "documentId": "{{docId1}}",
                      "documentName": "doc1.pdf",
                      "pageNumber": 1,
                      "paragraphReference": "¶1",
                      "snippet": "old text",
                      "confidenceScore": 0.85
                    }
                  ]
                }
              ],
              "risks": [],
              "recommendations": [],
              "sources": []
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json,
                new TokenUsage(120, 60, 0.002m),
                "gpt-4o")));

        var result = await CreateSut().CompareAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveOverview.Should().Be("Two documents compared");
        result.Value.Differences.Should().HaveCount(1);
        result.Value.Differences[0].Type.Should().Be(DifferenceType.Modified);
        result.Value.Differences[0].Before.Should().Be("old text");
        result.Value.Differences[0].After.Should().Be("new text");
    }

    [Fact]
    public async Task CompareAsync_WhenProviderFails_ReturnsError()
    {
        var (docId1, docId2) = ArrangeTwoOwnedDocuments();
        var request = new ComparisonRequest([docId1, docId2], "Version");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AiCompletionResult>(
                Error.Failure("provider.error", "unavailable")));

        var result = await CreateSut().CompareAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("provider.error");
    }

    [Theory]
    [InlineData("Added", DifferenceType.Added)]
    [InlineData("Removed", DifferenceType.Removed)]
    [InlineData("Modified", DifferenceType.Modified)]
    [InlineData("unknown", DifferenceType.Modified)]
    public async Task CompareAsync_DifferenceTypeMappings_AreCorrect(
        string jsonType, DifferenceType expectedType)
    {
        var (docId1, docId2) = ArrangeTwoOwnedDocuments();
        var request = new ComparisonRequest([docId1, docId2], "Contract");

        _searchMock
            .Setup(s => s.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SearchHit>>([]));

        var json = $$"""
            {
              "executiveOverview": "overview",
              "differences": [
                {
                  "type": "{{jsonType}}",
                  "section": "S",
                  "before": null,
                  "after": null,
                  "summary": "s",
                  "citations": []
                }
              ],
              "risks": [],
              "recommendations": [],
              "sources": []
            }
            """;

        _providerMock
            .Setup(p => p.CompleteAsync(It.IsAny<AiCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new AiCompletionResult(
                json, new TokenUsage(10, 5, 0m), "gpt-4o")));

        var result = await CreateSut().CompareAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Differences[0].Type.Should().Be(expectedType);
    }
}
