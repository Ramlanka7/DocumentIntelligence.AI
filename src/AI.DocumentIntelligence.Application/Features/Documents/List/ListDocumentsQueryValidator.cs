using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Documents.List;

/// <summary>Validates <see cref="ListDocumentsQuery"/> — no additional rules beyond dispatch.</summary>
internal sealed class ListDocumentsQueryValidator : AbstractValidator<ListDocumentsQuery>
{
    public ListDocumentsQueryValidator()
    {
        // No rules needed; the query carries no input parameters.
    }
}
