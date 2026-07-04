namespace AI.DocumentIntelligence.Application.Features.Admin.ListUsers;

/// <summary>Summary view of a user for admin list endpoints.</summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's assigned role name.</param>
/// <param name="IsActive">Whether the user account is currently active.</param>
public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string Role,
    bool IsActive);
