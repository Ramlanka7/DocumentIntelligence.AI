using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Export;

namespace AI.DocumentIntelligence.Application.Features.Export.ExportAnalysis;

/// <summary>
/// Exports an existing <see cref="AnalysisResult"/> to the requested <see cref="ExportFormat"/>.
/// The handler delegates entirely to <see cref="Abstractions.Export.IExportService"/>;
/// no new analysis logic is performed.
/// </summary>
/// <param name="Result">The analysis result to export.</param>
/// <param name="Format">The target file format.</param>
/// <param name="Title">Optional report title (defaults to "Analysis Report").</param>
public sealed record ExportAnalysisCommand(
    AnalysisResult Result,
    ExportFormat Format,
    string? Title = null) : ICommand<ExportDocumentResult>;
