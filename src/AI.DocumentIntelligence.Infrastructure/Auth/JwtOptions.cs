namespace AI.DocumentIntelligence.Infrastructure.Auth;

/// <summary>Strongly-typed binding for the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>A 256-bit (32-byte) base-64 or plain secret key. Store in user-secrets in dev.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 15;

    public int RefreshTokenExpiryDays { get; set; } = 7;
}
