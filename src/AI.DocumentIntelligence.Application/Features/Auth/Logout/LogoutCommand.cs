using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Auth.Logout;

/// <summary>
/// Revokes the current user's refresh token, ending all refresh-based sessions.
/// The user identity is resolved from <c>ICurrentUser</c> inside the handler.
/// </summary>
public sealed record LogoutCommand : ICommand;
