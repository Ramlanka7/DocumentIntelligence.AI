using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Documents.Delete;

/// <summary>Validates <see cref="DeleteDocumentCommand"/>.</summary>
internal sealed class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Document ID is required.");
    }
}
