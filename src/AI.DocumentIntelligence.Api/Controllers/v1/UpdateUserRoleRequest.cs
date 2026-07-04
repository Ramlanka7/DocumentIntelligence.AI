namespace AI.DocumentIntelligence.Api.Controllers.v1;

/// <summary>Request body for updating a platform user's assigned role.</summary>
/// <param name="Role">The new role name.</param>
public sealed record UpdateUserRoleRequest(string Role);
