namespace AI.DocumentIntelligence.Application.Contracts;

/// <summary>
/// A traceable reference back to the exact source location that supports an AI-produced statement.
/// Every analysis, comparison and chat response on the platform must carry citations.
/// </summary>
/// <param name="DocumentId">Identifier of the source document.</param>
/// <param name="DocumentName">Human-readable name of the source document.</param>
/// <param name="PageNumber">1-based page the supporting text appears on.</param>
/// <param name="ParagraphReference">Paragraph/section locator within the page (e.g. "¶3" or "2.1").</param>
/// <param name="Snippet">The supporting excerpt quoted from the source.</param>
/// <param name="ConfidenceScore">Model confidence in the citation, from 0.0 to 1.0.</param>
public sealed record Citation(
    Guid DocumentId,
    string DocumentName,
    int PageNumber,
    string ParagraphReference,
    string Snippet,
    double ConfidenceScore);
