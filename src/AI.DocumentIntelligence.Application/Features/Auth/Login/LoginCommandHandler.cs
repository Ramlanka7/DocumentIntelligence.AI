using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Auth.Login;

/// <summary>
/// Verifies credentials, issues JWT + refresh tokens, persists the hashed refresh token,
/// and records a "User.LoggedIn" audit entry.
/// </summary>
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IAuditService auditService)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(DomainErrors.User.InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(DomainErrors.User.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(DomainErrors.User.Inactive);
        }

        var now = DateTimeOffset.UtcNow;
        var accessToken = tokenService.GenerateAccessToken(user);
        var plainRefreshToken = tokenService.GenerateRefreshToken();
        var hashedRefreshToken = tokenService.HashToken(plainRefreshToken);
        var expiresAt = now.Add(tokenService.AccessTokenExpiry);
        var refreshExpiresAt = now.Add(tokenService.RefreshTokenExpiry);

        user.SetRefreshToken(hashedRefreshToken, refreshExpiresAt);
        userRepository.Update(user);

        await auditService.LogAsync(
            action: "User.LoggedIn",
            entityType: "User",
            entityId: user.Id,
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(accessToken, plainRefreshToken, expiresAt));
    }
}
