namespace AI.DocumentIntelligence.Application.Features.Admin.GetUser;

/// <summary>Detailed view of a user for admin single-user endpoints.</summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's assigned role name.</param>
/// <param name="IsActive">Whether the user account is currently active.</param>
/// <param name="CreatedAt">When the user account was created (UTC).</param>
public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
