namespace AI.DocumentIntelligence.Application.Abstractions.Identity;

/// <summary>
/// Provides information about the user making the current request. Implemented in the API layer over
/// the HTTP context so handlers can apply authorization and audit without depending on ASP.NET.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's identifier, or null when unauthenticated.</summary>
    public Guid? UserId { get; }

    /// <summary>The authenticated user's email, or null when unavailable.</summary>
    public string? Email { get; }

    /// <summary>The roles assigned to the current user.</summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>Whether the current request is authenticated.</summary>
    public bool IsAuthenticated { get; }
}
