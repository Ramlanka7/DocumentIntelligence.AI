using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Analysis;

/// <summary>Validates <see cref="AnalyzeDocumentsCommand"/>.</summary>
internal sealed class AnalyzeDocumentsCommandValidator : AbstractValidator<AnalyzeDocumentsCommand>
{
    public AnalyzeDocumentsCommandValidator()
    {
        RuleFor(x => x.DocumentIds)
            .NotNull().WithMessage("Document IDs are required.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one document ID is required.")
            .Must(ids => ids.Count <= 4).WithMessage("A maximum of 4 document IDs is allowed.");

        RuleFor(x => x.Capability)
            .NotEmpty().WithMessage("Capability is required.");
    }
}
