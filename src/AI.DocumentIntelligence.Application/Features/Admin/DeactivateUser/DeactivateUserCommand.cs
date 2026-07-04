using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Admin.DeactivateUser;

/// <summary>Deactivates a platform user account, preventing them from logging in. Admin-only.</summary>
/// <param name="Id">The user's unique identifier.</param>
public sealed record DeactivateUserCommand(Guid Id) : ICommand;
