namespace AI.DocumentIntelligence.Application.Contracts.Documents;

/// <summary>A tabular structure detected within a document, as rows of cell values.</summary>
/// <param name="PageNumber">1-based page on which the table appears.</param>
/// <param name="Rows">The table rows; each row is an ordered list of cell values.</param>
public sealed record ExtractedTable(int PageNumber, IReadOnlyList<IReadOnlyList<string>> Rows);
