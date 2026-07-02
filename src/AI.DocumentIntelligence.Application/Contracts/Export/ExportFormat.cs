namespace AI.DocumentIntelligence.Application.Contracts.Export;

/// <summary>
/// The output format requested for an export operation.
/// Each format targets a specific use case: PDF for read-only sharing, Word for further editing,
/// Excel for tabular data analysis, and Markdown for plain-text pipelines.
/// </summary>
public enum ExportFormat
{
    /// <summary>Adobe PDF — read-only, paginated, suitable for printing and sharing.</summary>
    Pdf = 0,

    /// <summary>Microsoft Word DOCX — editable rich-text document.</summary>
    Word = 1,

    /// <summary>Microsoft Excel XLSX — tabular view across multiple worksheets.</summary>
    Excel = 2,

    /// <summary>Markdown (.md) — plain-text format for developer and documentation workflows.</summary>
    Markdown = 3,
}
