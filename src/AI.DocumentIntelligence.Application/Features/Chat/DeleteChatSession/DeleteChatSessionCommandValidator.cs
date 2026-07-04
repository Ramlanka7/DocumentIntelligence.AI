using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Chat.DeleteChatSession;

/// <summary>Validates <see cref="DeleteChatSessionCommand"/>.</summary>
internal sealed class DeleteChatSessionCommandValidator : AbstractValidator<DeleteChatSessionCommand>
{
    public DeleteChatSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
