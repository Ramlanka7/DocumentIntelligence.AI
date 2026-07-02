using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;

namespace AI.DocumentIntelligence.Infrastructure.Export;

/// <summary>
/// Internal strategy interface for format-specific document generation.
/// Each concrete formatter handles exactly one <see cref="ExportFormat"/> and produces
/// the appropriate binary content with citations preserved.
/// </summary>
internal interface IExportFormatter
{
    /// <summary>The <see cref="ExportFormat"/> this formatter handles.</summary>
    internal ExportFormat Format { get; }

    /// <summary>Generates a formatted document from an analysis result.</summary>
    internal ExportDocumentResult FormatAnalysis(AnalysisResult result, string title);

    /// <summary>Generates a formatted document from a comparison result.</summary>
    internal ExportDocumentResult FormatComparison(ComparisonResult result, string title);
}
