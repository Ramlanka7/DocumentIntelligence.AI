using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Admin.UpdateUserRole;

/// <summary>Updates the role assigned to a platform user. Admin-only.</summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Role">The new role name (Admin, Analyst, or Viewer).</param>
public sealed record UpdateUserRoleCommand(Guid Id, string Role) : ICommand;
