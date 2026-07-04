using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Admin.ListUsers;

/// <summary>Returns a summary list of all platform users. Admin-only.</summary>
public sealed record ListUsersQuery : IQuery<IReadOnlyList<UserSummaryDto>>;
