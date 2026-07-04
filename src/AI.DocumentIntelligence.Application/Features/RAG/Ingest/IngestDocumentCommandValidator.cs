using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.RAG.Ingest;

internal sealed class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("DocumentId must be a non-empty GUID.");

        RuleFor(x => x.DocumentName)
            .NotEmpty()
            .WithMessage("DocumentName is required.");

        RuleFor(x => x.ExtractionResult)
            .NotNull()
            .WithMessage("ExtractionResult must be provided.");
    }
}
