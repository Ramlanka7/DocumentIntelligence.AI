using System.Security.Claims;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace AI.DocumentIntelligence.Api.Infrastructure;

/// <summary>
/// Reads the authenticated user's identity from the ASP.NET Core <see cref="ClaimsPrincipal"/>
/// on the current HTTP context. Registered as scoped so it reflects the per-request principal.
/// </summary>
internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Principal?.FindFirstValue("sub");

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)[];

    /// <inheritdoc />
    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated is true;

    /// <inheritdoc />
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
