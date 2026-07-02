using AI.DocumentIntelligence.Application.Abstractions.Export;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Export;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Export.ExportComparison;

/// <summary>
/// Handles <see cref="ExportComparisonCommand"/> by delegating to <see cref="IExportService"/>.
/// The handler is intentionally thin — all formatting logic lives in the Infrastructure layer.
/// </summary>
internal sealed class ExportComparisonCommandHandler(IExportService exportService)
    : ICommandHandler<ExportComparisonCommand, ExportDocumentResult>
{
    /// <inheritdoc />
    public Task<Result<ExportDocumentResult>> Handle(
        ExportComparisonCommand request,
        CancellationToken cancellationToken) =>
        exportService.ExportComparisonAsync(
            request.Result,
            request.Format,
            request.Title,
            cancellationToken);
}
