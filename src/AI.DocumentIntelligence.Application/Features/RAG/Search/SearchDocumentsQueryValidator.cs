using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.RAG.Search;

internal sealed class SearchDocumentsQueryValidator : AbstractValidator<SearchDocumentsQuery>
{
    public SearchDocumentsQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Query must not be empty.")
            .MaximumLength(2000)
            .WithMessage("Query must not exceed 2000 characters.");

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 50)
            .WithMessage("TopK must be between 1 and 50.");
    }
}
