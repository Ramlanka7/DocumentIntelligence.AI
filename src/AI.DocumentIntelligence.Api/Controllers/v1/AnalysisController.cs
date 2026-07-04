using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Features.Analysis;
using AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>Triggers AI-powered document analysis for one or more documents.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analysis")]
[Authorize(Policy = "AnalystOrAbove")]
[Produces("application/json")]
public sealed class AnalysisController(ISender sender) : ControllerBase
{
    /// <summary>Analyses one or more documents for a given capability.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AnalyzeAsync(
        [FromBody] AnalyzeDocumentsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns a summary list of the current user's analysis sessions.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalysisSessionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAnalysisSessionsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }
}
