using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;

/// <summary>No inputs to validate — ownership is enforced in the handler via ICurrentUser.</summary>
internal sealed class GetComparisonSessionsQueryValidator : AbstractValidator<GetComparisonSessionsQuery>
{
    public GetComparisonSessionsQueryValidator()
    {
    }
}
