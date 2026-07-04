using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Documents.Queries;

/// <summary>Validates <see cref="GetDocumentQuery"/>.</summary>
internal sealed class GetDocumentQueryValidator : AbstractValidator<GetDocumentQuery>
{
    public GetDocumentQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Document ID is required.");
    }
}
