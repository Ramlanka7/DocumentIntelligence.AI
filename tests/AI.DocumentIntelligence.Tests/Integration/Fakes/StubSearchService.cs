using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>
/// Stub search service that returns an empty hit-list for every query.
/// Prevents any real Azure AI Search calls in integration tests.
/// </summary>
public sealed class StubSearchService : ISearchService
{
    public Task<Result<IReadOnlyList<SearchHit>>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<IReadOnlyList<SearchHit>>([]));

    public Task<Result> IndexAsync(
        IReadOnlyList<SearchableChunk> chunks,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());

    public Task<Result> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}
