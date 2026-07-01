using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Features.Auth.Login;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Auth.Refresh;

/// <summary>
/// Validates the incoming refresh token by looking up its hash, rotates the token pair,
/// and records an audit entry. The user ID is never accepted from the caller — it is
/// resolved solely from the stored hash to prevent token-hijacking enumeration.
/// </summary>
internal sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IAuditService auditService)
    : ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var incomingHash = tokenService.HashToken(request.RefreshToken);

        var user = await userRepository.GetByRefreshTokenHashAsync(incomingHash, cancellationToken);

        if (user is null
            || user.RefreshTokenHash is null
            || user.RefreshTokenExpiresAtUtc is null
            || user.RefreshTokenExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            return Result.Failure<LoginResponse>(DomainErrors.Token.Invalid);
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
            action: "User.TokenRefreshed",
            entityType: "User",
            entityId: user.Id,
            ct: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(accessToken, plainRefreshToken, expiresAt));
    }
}
