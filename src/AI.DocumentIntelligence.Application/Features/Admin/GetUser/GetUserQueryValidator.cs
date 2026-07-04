using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetUser;

/// <summary>Validates <see cref="GetUserQuery"/>.</summary>
internal sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
