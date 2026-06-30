namespace AI.DocumentIntelligence.Application.Contracts.Search;

/// <summary>A retrieval request against the vector/hybrid search index.</summary>
/// <param name="Query">The natural-language query text.</param>
/// <param name="DocumentIds">Documents to restrict the search to; empty means all accessible documents.</param>
/// <param name="TopK">Maximum number of hits to return.</param>
/// <param name="UseHybrid">When true, combine vector similarity with keyword search.</param>
public sealed record SearchRequest(
    string Query,
    IReadOnlyList<Guid> DocumentIds,
    int TopK = 5,
    bool UseHybrid = true);
