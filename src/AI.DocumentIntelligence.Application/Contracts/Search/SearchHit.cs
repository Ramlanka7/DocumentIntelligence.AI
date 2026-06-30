namespace AI.DocumentIntelligence.Application.Contracts.Search;

/// <summary>A single retrieved chunk from the search index, with enough locator data to build a citation.</summary>
/// <param name="DocumentId">The document the chunk belongs to.</param>
/// <param name="DocumentName">Human-readable document name.</param>
/// <param name="PageNumber">1-based page the chunk came from.</param>
/// <param name="ParagraphReference">Paragraph/section locator within the page.</param>
/// <param name="Content">The chunk text.</param>
/// <param name="Score">Relevance score assigned by the search engine.</param>
public sealed record SearchHit(
    Guid DocumentId,
    string DocumentName,
    int PageNumber,
    string ParagraphReference,
    string Content,
    double Score);
