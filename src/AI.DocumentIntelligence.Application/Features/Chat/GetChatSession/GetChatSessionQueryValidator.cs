using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;

/// <summary>Validates <see cref="GetChatSessionQuery"/>.</summary>
internal sealed class GetChatSessionQueryValidator : AbstractValidator<GetChatSessionQuery>
{
    public GetChatSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
