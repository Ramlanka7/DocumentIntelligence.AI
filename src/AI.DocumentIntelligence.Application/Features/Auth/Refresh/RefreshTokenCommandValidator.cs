using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Auth.Refresh;

/// <summary>Validates the <see cref="RefreshTokenCommand"/> before it reaches the handler.</summary>
internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
