using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Search;

/// <summary>
/// Abstraction over the vector/hybrid search index (e.g. Azure AI Search) used by the RAG pipeline
/// to store and retrieve document chunks.
/// </summary>
public interface ISearchService
{
    /// <summary>Upserts a batch of embedded chunks into the index.</summary>
    /// <param name="chunks">The chunks to index.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A success or failure <see cref="Result"/>.</returns>
    public Task<Result> IndexAsync(
        IReadOnlyList<SearchableChunk> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves the most relevant chunks for a query using vector and/or keyword search.</summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The ranked hits, or a failure <see cref="Result"/>.</returns>
    public Task<Result<IReadOnlyList<SearchHit>>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Removes all indexed chunks belonging to a document.</summary>
    /// <param name="documentId">The document whose chunks should be removed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A success or failure <see cref="Result"/>.</returns>
    public Task<Result> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
