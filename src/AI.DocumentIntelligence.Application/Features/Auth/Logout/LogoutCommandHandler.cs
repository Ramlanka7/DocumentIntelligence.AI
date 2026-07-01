using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Auth.Logout;

/// <summary>
/// Revokes the current user's refresh token and records a "User.LoggedOut" audit entry.
/// </summary>
internal sealed class LogoutCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAuditService auditService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(DomainErrors.User.InvalidCredentials);
        }

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        user.RevokeRefreshToken();
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            action: "User.LoggedOut",
            entityType: "User",
            entityId: user.Id,
            ct: cancellationToken);

        return Result.Success();
    }
}
