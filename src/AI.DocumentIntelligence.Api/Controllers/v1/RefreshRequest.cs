namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>
/// Optional body for POST /auth/refresh. Browser clients send an empty body — the refresh
/// token travels in an HttpOnly cookie. Non-browser clients (CLI, tests) may pass the token
/// explicitly instead.
/// </summary>
/// <param name="RefreshToken">The plain-text refresh token, when not using the cookie.</param>
public sealed record RefreshRequest(string? RefreshToken);
