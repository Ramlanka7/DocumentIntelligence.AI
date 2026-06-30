namespace AI.DocumentIntelligence.Application.Contracts.Documents;

/// <summary>A logical section/heading detected within a document.</summary>
/// <param name="Heading">The section heading text.</param>
/// <param name="Level">Heading depth (1 = top level).</param>
/// <param name="StartPage">1-based page on which the section begins.</param>
/// <param name="Content">Plain-text content belonging to the section.</param>
public sealed record ExtractedSection(string Heading, int Level, int StartPage, string Content);
