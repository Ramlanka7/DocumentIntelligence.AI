using AI.DocumentIntelligence.Application.Abstractions.Export;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Infrastructure.Export;

/// <summary>
/// Orchestrates format-specific export by resolving the correct <see cref="IExportFormatter"/>
/// for the requested <see cref="ExportFormat"/> and offloading the CPU-bound generation to the
/// thread pool so the request pipeline remains non-blocking for large documents.
/// All exceptions from formatters are caught and returned as <see cref="DomainErrors.Export"/> failures.
/// </summary>
internal sealed partial class ExportService : IExportService
{
    private readonly Dictionary<ExportFormat, IExportFormatter> _formatters;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IEnumerable<IExportFormatter> formatters,
        ILogger<ExportService> logger)
    {
        _formatters = formatters.ToDictionary(f => f.Format);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ExportDocumentResult>> ExportAnalysisAsync(
        AnalysisResult analysisResult,
        ExportFormat format,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (analysisResult is null)
        {
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.EmptyResult);
        }

        if (!_formatters.TryGetValue(format, out var formatter))
        {
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.UnsupportedFormat);
        }

        var reportTitle = string.IsNullOrWhiteSpace(title) ? "Analysis Report" : title;
        LogStartingExport(_logger, format, "analysis");

        try
        {
            var result = await Task.Run(
                () => formatter.FormatAnalysis(analysisResult, reportTitle),
                cancellationToken);

            LogExportCompleted(_logger, format, result.Content.Length);
            return Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw; // propagate cancellation — not an application error
        }
        catch (Exception ex)
        {
            LogExportFailed(_logger, format, ex);
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.ExportFailed(ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ExportDocumentResult>> ExportComparisonAsync(
        ComparisonResult comparisonResult,
        ExportFormat format,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (comparisonResult is null)
        {
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.EmptyResult);
        }

        if (!_formatters.TryGetValue(format, out var formatter))
        {
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.UnsupportedFormat);
        }

        var reportTitle = string.IsNullOrWhiteSpace(title) ? "Comparison Report" : title;
        LogStartingExport(_logger, format, "comparison");

        try
        {
            var result = await Task.Run(
                () => formatter.FormatComparison(comparisonResult, reportTitle),
                cancellationToken);

            LogExportCompleted(_logger, format, result.Content.Length);
            return Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw; // propagate cancellation — not an application error
        }
        catch (Exception ex)
        {
            LogExportFailed(_logger, format, ex);
            return Result.Failure<ExportDocumentResult>(DomainErrors.Export.ExportFailed("An unexpected error occurred during export generation."));
        }
    }

    // ---- source-generated log methods (CA1873-safe: args evaluated only when logging is enabled) -----

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting {Format} export for {Kind} result")]
    private static partial void LogStartingExport(ILogger logger, ExportFormat format, string kind);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Export ({Format}) completed — {Bytes} bytes")]
    private static partial void LogExportCompleted(ILogger logger, ExportFormat format, int bytes);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Export ({Format}) failed")]
    private static partial void LogExportFailed(ILogger logger, ExportFormat format, Exception ex);
}
