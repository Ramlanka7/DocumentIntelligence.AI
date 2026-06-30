namespace AI.DocumentIntelligence.Application.Contracts.Documents;

/// <summary>The text content of a single page of a processed document.</summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="Text">Plain-text content of the page.</param>
public sealed record ExtractedPage(int PageNumber, string Text);
