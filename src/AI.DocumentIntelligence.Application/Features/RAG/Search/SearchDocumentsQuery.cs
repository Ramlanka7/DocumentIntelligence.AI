using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.RAG.Search;

/// <summary>
/// Returns the top-k most relevant document chunks for a natural-language query, with scores
/// that are mapped to <see cref="SearchDocumentsResponse"/> for citation construction downstream.
/// </summary>
/// <param name="Query">The natural-language query.</param>
/// <param name="DocumentIds">
/// When non-empty, restricts retrieval to these documents only.
/// Pass an empty collection to search across all accessible documents.
/// </param>
/// <param name="TopK">Maximum number of chunks to return.</param>
/// <param name="UseHybrid">
/// When <see langword="true"/>, combines vector similarity with keyword BM25 scoring
/// (hybrid search). Set to <see langword="false"/> for pure vector search.
/// </param>
public sealed record SearchDocumentsQuery(
    string Query,
    IReadOnlyList<Guid> DocumentIds,
    int TopK = 5,
    bool UseHybrid = true) : IQuery<SearchDocumentsResponse>;
