using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Documents.Upload;

/// <summary>Validates <see cref="UploadDocumentCommand"/> before it reaches the handler.</summary>
internal sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] SupportedExtensions =
        [".pdf", ".docx", ".txt", ".csv", ".pptx"];

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name => SupportedExtensions.Contains(
                Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Unsupported file type. Allowed: .pdf, .docx, .txt, .csv, .pptx.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than zero.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("File content stream is required.");
    }
}
