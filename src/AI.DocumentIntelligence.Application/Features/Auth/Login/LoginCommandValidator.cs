using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Auth.Login;

/// <summary>Validates the <see cref="LoginCommand"/> before it reaches the handler.</summary>
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
