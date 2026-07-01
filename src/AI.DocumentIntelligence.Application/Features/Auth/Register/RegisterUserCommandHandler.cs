using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Auth.Register;

/// <summary>
/// Creates a new platform user after verifying the email is not already taken.
/// Password is hashed via <see cref="IPasswordHasher"/> before storage.
/// </summary>
internal sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(DomainErrors.User.EmailAlreadyInUse);
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(request.Email, passwordHash, request.FullName, request.Role);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
