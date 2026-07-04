using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Features.Comparison;
using AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>Triggers AI-powered side-by-side or version comparison of two to four documents.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/comparison")]
[Authorize(Policy = "AnalystOrAbove")]
[Produces("application/json")]
public sealed class ComparisonController(ISender sender) : ControllerBase
{
    /// <summary>Compares two to four documents using the specified comparison type.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompareAsync(
        [FromBody] CompareDocumentsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns a summary list of the current user's comparison sessions.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<ComparisonSessionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetComparisonSessionsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }
}
