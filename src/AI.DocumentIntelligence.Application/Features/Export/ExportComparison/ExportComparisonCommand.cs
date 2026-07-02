using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;

namespace AI.DocumentIntelligence.Application.Features.Export.ExportComparison;

/// <summary>
/// Exports an existing <see cref="ComparisonResult"/> to the requested <see cref="ExportFormat"/>.
/// The handler delegates entirely to <see cref="Abstractions.Export.IExportService"/>;
/// no new comparison logic is performed.
/// </summary>
/// <param name="Result">The comparison result to export.</param>
/// <param name="Format">The target file format.</param>
/// <param name="Title">Optional report title (defaults to "Comparison Report").</param>
public sealed record ExportComparisonCommand(
    ComparisonResult Result,
    ExportFormat Format,
    string? Title = null) : ICommand<ExportDocumentResult>;
