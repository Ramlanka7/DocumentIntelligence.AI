namespace AI.DocumentIntelligence.Application.Contracts.Documents;

/// <summary>
/// The full output of an <c>IDocumentProcessor</c>: the document's text together with the structural
/// elements (pages, sections, tables) and metadata needed downstream for chunking and citation.
/// </summary>
/// <param name="FullText">Concatenated plain text of the entire document.</param>
/// <param name="Pages">Per-page text content.</param>
/// <param name="Sections">Detected headings/sections.</param>
/// <param name="Tables">Detected tables.</param>
/// <param name="Metadata">Document metadata.</param>
public sealed record DocumentExtractionResult(
    string FullText,
    IReadOnlyList<ExtractedPage> Pages,
    IReadOnlyList<ExtractedSection> Sections,
    IReadOnlyList<ExtractedTable> Tables,
    DocumentMetadata Metadata);
