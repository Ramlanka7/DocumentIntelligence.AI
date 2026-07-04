using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Features.Admin.DeactivateUser;
using AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;
using AI.DocumentIntelligence.Application.Features.Admin.GetUser;
using AI.DocumentIntelligence.Application.Features.Admin.ListUsers;
using AI.DocumentIntelligence.Application.Features.Admin.UpdateUserRole;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>Admin-only user management endpoints.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    /// <summary>Returns a summary list of all platform users.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsersAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListUsersQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns the detail view of a single user.</summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Updates the role assigned to a platform user.</summary>
    [HttpPut("users/{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoleAsync(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserRoleCommand(id, request.Role), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Returns aggregated platform metrics for the admin dashboard.</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardMetricsAsync(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] string? operationType,
        [FromQuery] Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardMetricsQuery(dateFrom, dateTo, operationType, userId);
        var result = await sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Deactivates a platform user, preventing them from logging in.</summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}

