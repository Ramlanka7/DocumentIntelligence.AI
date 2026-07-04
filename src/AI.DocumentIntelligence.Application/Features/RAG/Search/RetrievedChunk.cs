namespace AI.DocumentIntelligence.Application.Features.RAG.Search;

/// <summary>A single ranked document chunk returned from the RAG retrieval step.</summary>
/// <param name="DocumentId">The owning document.</param>
/// <param name="DocumentName">Human-readable document name.</param>
/// <param name="PageNumber">1-based page the chunk originated from.</param>
/// <param name="ParagraphReference">Paragraph/section locator within the page.</param>
/// <param name="Content">The chunk text.</param>
/// <param name="RelevanceScore">The raw score assigned by the search engine (higher = more relevant).</param>
public sealed record RetrievedChunk(
    Guid DocumentId,
    string DocumentName,
    int PageNumber,
    string ParagraphReference,
    string Content,
    double RelevanceScore);
