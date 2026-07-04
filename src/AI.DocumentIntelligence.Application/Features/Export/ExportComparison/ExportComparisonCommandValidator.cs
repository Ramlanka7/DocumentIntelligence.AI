using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Export.ExportComparison;

/// <summary>Validates <see cref="ExportComparisonCommand"/> before it reaches the handler.</summary>
internal sealed class ExportComparisonCommandValidator : AbstractValidator<ExportComparisonCommand>
{
    public ExportComparisonCommandValidator()
    {
        RuleFor(x => x.Result)
            .NotNull().WithMessage("A comparison result is required.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("Export format must be one of: Pdf, Word, Excel, Markdown.");
    }
}
