using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Export;

/// <summary>
/// Converts a structured analysis or comparison result into a downloadable document in the requested
/// <see cref="ExportFormat"/>. Implementations live in the Infrastructure layer (one per format or as
/// a single orchestrating service backed by format-specific formatters).
/// Every generated document must preserve citations from the source result.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Generates a downloadable document from the supplied <paramref name="analysisResult"/>.
    /// </summary>
    /// <param name="analysisResult">The analysis result to export. Must not be <see langword="null"/>.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="title">Optional document title; defaults to "Analysis Report".</param>
    /// <param name="cancellationToken">Propagates a cancellation signal.</param>
    public Task<Result<ExportDocumentResult>> ExportAnalysisAsync(
        AnalysisResult analysisResult,
        ExportFormat format,
        string? title = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a downloadable document from the supplied <paramref name="comparisonResult"/>.
    /// </summary>
    /// <param name="comparisonResult">The comparison result to export. Must not be <see langword="null"/>.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="title">Optional document title; defaults to "Comparison Report".</param>
    /// <param name="cancellationToken">Propagates a cancellation signal.</param>
    public Task<Result<ExportDocumentResult>> ExportComparisonAsync(
        ComparisonResult comparisonResult,
        ExportFormat format,
        string? title = null,
        CancellationToken cancellationToken = default);
}
