using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Admin.DeactivateUser;

/// <summary>Validates <see cref="DeactivateUserCommand"/>.</summary>
internal sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
