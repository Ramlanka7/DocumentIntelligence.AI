using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetUser;

/// <summary>Returns full user detail for admin endpoints, or <see cref="DomainErrors.User.NotFound"/> when absent.</summary>
internal sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, UserDetailDto>
{
    public async Task<Result<UserDetailDto>> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDetailDto>(DomainErrors.User.NotFound);
        }

        var dto = new UserDetailDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsActive,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));

        return Result.Success(dto);
    }
}
