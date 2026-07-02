using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Features.Export.ExportAnalysis;
using AI.DocumentIntelligence.Application.Features.Export.ExportComparison;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>
/// Converts an existing analysis or comparison result to a downloadable file in the
/// requested format (PDF, Word, Excel, or Markdown). All generated documents preserve
/// citations. The caller first obtains a result from the analysis or comparison endpoint,
/// then posts it here with a format choice to receive the file.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/export")]
[Authorize(Policy = "AnalystOrAbove")]
public sealed class ExportController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Exports an analysis result to the requested format and returns a downloadable file.
    /// </summary>
    /// <remarks>
    /// The request body must contain the full <c>AnalysisResult</c> as returned by
    /// <c>POST /api/v1/analysis</c>, plus the desired <c>format</c> (0=Pdf, 1=Word, 2=Excel, 3=Markdown)
    /// and an optional <c>title</c>.
    /// </remarks>
    [HttpPost("analysis")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAnalysisAsync(
        [FromBody] ExportAnalysisCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var export = result.Value;
        return File(export.Content, export.ContentType, export.FileName);
    }

    /// <summary>
    /// Exports a comparison result to the requested format and returns a downloadable file.
    /// </summary>
    /// <remarks>
    /// The request body must contain the full <c>ComparisonResult</c> as returned by
    /// <c>POST /api/v1/comparison</c>, plus the desired <c>format</c> (0=Pdf, 1=Word, 2=Excel, 3=Markdown)
    /// and an optional <c>title</c>.
    /// </remarks>
    [HttpPost("comparison")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportComparisonAsync(
        [FromBody] ExportComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        var export = result.Value;
        return File(export.Content, export.ContentType, export.FileName);
    }
}
