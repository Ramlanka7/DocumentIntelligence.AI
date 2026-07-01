namespace AI.DocumentIntelligence.Application.Features.Auth.Login;

/// <summary>Returned by a successful login or token refresh.</summary>
/// <param name="AccessToken">A signed JWT that should be sent as a Bearer token.</param>
/// <param name="RefreshToken">A long-lived opaque token used to obtain a new access token.</param>
/// <param name="ExpiresAt">The UTC instant at which the access token expires.</param>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
