using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Auth.Login;

/// <summary>Authenticates a user with email/password credentials and issues JWT tokens.</summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The plain-text password (hashed inside the handler; never stored).</param>
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
