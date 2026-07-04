using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Comparison;

/// <summary>Validates <see cref="CompareDocumentsCommand"/>.</summary>
internal sealed class CompareDocumentsCommandValidator : AbstractValidator<CompareDocumentsCommand>
{
    public CompareDocumentsCommandValidator()
    {
        RuleFor(x => x.DocumentIds)
            .NotNull().WithMessage("Document IDs are required.")
            .Must(ids => ids.Count >= 2).WithMessage("A comparison requires at least 2 documents.")
            .Must(ids => ids.Count <= 4).WithMessage("A maximum of 4 documents may be compared.");

        RuleFor(x => x.ComparisonType)
            .NotEmpty().WithMessage("Comparison type is required.");
    }
}
