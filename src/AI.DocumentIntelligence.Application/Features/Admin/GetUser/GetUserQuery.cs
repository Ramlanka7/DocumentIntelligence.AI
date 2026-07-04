using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetUser;

/// <summary>Returns the full detail view of a single user. Admin-only.</summary>
/// <param name="Id">The user's unique identifier.</param>
public sealed record GetUserQuery(Guid Id) : IQuery<UserDetailDto>;
