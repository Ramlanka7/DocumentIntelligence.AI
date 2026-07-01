using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Application.Features.Auth.Register;

/// <summary>Creates a new platform user. Restricted to Admin callers.</summary>
/// <param name="Email">The new user's email address (must be unique).</param>
/// <param name="Password">The plain-text password; hashed inside the handler.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Role">The role to assign.</param>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    UserRole Role) : ICommand<Guid>;
