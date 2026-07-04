using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Admin.ListUsers;

/// <summary>Returns all users in the system for admin management.</summary>
internal sealed class ListUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<ListUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    public async Task<Result<IReadOnlyList<UserSummaryDto>>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        var dtos = users
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.Role.ToString(),
                u.IsActive))
            .ToList();

        return Result.Success<IReadOnlyList<UserSummaryDto>>(dtos);
    }
}
