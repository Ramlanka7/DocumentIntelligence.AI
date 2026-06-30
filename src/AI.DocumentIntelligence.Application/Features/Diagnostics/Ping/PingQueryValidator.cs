using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Diagnostics.Ping;

/// <summary>Validates <see cref="PingQuery"/>; demonstrates the validation pipeline behavior.</summary>
public sealed class PingQueryValidator : AbstractValidator<PingQuery>
{
    /// <summary>Configures the rules for <see cref="PingQuery"/>.</summary>
    public PingQueryValidator()
    {
        RuleFor(query => query.Message)
            .NotEmpty()
            .WithMessage("Message must not be empty.")
            .MaximumLength(1_000);
    }
}
