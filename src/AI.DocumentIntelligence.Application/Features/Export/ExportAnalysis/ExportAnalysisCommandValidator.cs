using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Export.ExportAnalysis;

/// <summary>Validates <see cref="ExportAnalysisCommand"/> before it reaches the handler.</summary>
internal sealed class ExportAnalysisCommandValidator : AbstractValidator<ExportAnalysisCommand>
{
    public ExportAnalysisCommandValidator()
    {
        RuleFor(x => x.Result)
            .NotNull().WithMessage("An analysis result is required.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("Export format must be one of: Pdf, Word, Excel, Markdown.");
    }
}
