using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Identity;

/// <summary>
/// Generates and validates cryptographic tokens for authentication. Implemented in the
/// Infrastructure layer so the Application layer stays free of JWT/crypto dependencies.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a signed JWT access token for the given user.</summary>
    public string GenerateAccessToken(User user);

    /// <summary>Creates a cryptographically random refresh token (plain-text, not stored).</summary>
    public string GenerateRefreshToken();

    /// <summary>Returns the SHA-256 hash of a plain refresh token for safe storage.</summary>
    public string HashToken(string token);

    /// <summary>How long an access token remains valid (from its issue time).</summary>
    public TimeSpan AccessTokenExpiry { get; }

    /// <summary>How long a refresh token remains valid (from its issue time).</summary>
    public TimeSpan RefreshTokenExpiry { get; }
}
