using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Chat;

/// <summary>Validates <see cref="ChatCommand"/>.</summary>
internal sealed class ChatCommandValidator : AbstractValidator<ChatCommand>
{
    public ChatCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.DocumentIds)
            .NotNull().WithMessage("Document IDs are required.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one document ID is required.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty.");

        RuleFor(x => x.History)
            .NotNull().WithMessage("History cannot be null.");
    }
}
