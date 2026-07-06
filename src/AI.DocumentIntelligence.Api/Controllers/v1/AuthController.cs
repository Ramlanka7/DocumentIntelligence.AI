using AI.DocumentIntelligence.Api.Extensions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Application.Features.Auth.Logout;
using AI.DocumentIntelligence.Application.Features.Auth.Refresh;
using AI.DocumentIntelligence.Application.Features.Auth.Register;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>
/// Manages user authentication: login, token refresh, logout, and registration.
///
/// The refresh token is delivered to browsers exclusively via an HttpOnly, Secure,
/// SameSite=Strict cookie scoped to this controller's path — JavaScript can never read it,
/// so an XSS compromise cannot exfiltrate the long-lived credential. The short-lived access
/// token is returned in the response body. Non-browser clients may instead pass the refresh
/// token in the request body.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender, ITokenService tokenService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    /// <summary>Authenticates a user and returns a JWT access token; sets the refresh cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthEndpoints")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(result.Value with { RefreshToken = string.Empty });
    }

    /// <summary>
    /// Issues a new access + refresh token pair. The refresh token is read from the HttpOnly
    /// cookie (browsers) or, when absent, from the request body (non-browser clients).
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthEndpoints")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(
        [FromBody] RefreshRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken =
            Request.Cookies[RefreshTokenCookieName]
            ?? request?.RefreshToken
            ?? string.Empty;

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(result.Value with { RefreshToken = string.Empty });
    }

    /// <summary>Revokes the current user's refresh token and clears the refresh cookie.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(), cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName, BuildRefreshCookieOptions(expires: null));

        return result.ToActionResult(this);
    }

    /// <summary>Creates a new platform user. Restricted to Admin role.</summary>
    [HttpPost("register")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return CreatedAtAction(nameof(AdminController.GetUserAsync), "Admin", new { id = result.Value, version = RouteData.Values["version"] }, result.Value);
    }

    private void SetRefreshTokenCookie(string refreshToken) =>
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            BuildRefreshCookieOptions(DateTimeOffset.UtcNow.Add(tokenService.RefreshTokenExpiry)));

    private CookieOptions BuildRefreshCookieOptions(DateTimeOffset? expires)
    {
        var isLocalhost = string.Equals(Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || Request.Host.Host.StartsWith("127.", StringComparison.Ordinal);

        return new CookieOptions
        {
            HttpOnly = true,
            // Secure cookies require HTTPS; allow plain-HTTP only for localhost dev.
            Secure = Request.IsHttps || !isLocalhost,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            // Scope the cookie to the auth endpoints only — it is never needed elsewhere,
            // so it is never transmitted elsewhere.
            Path = $"/api/v{RouteData.Values["version"]}/auth",
        };
    }
}
