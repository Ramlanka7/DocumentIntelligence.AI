using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Features.Auth.Login;

namespace AI.DocumentIntelligence.Application.Features.Auth.Refresh;

/// <summary>
/// Exchanges a valid refresh token for a new access token + rotated refresh token pair.
/// The user is resolved from the stored token hash — callers do not supply a user ID.
/// </summary>
/// <param name="RefreshToken">The plain-text refresh token issued at login or last refresh.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<LoginResponse>;
