using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;

/// <summary>No inputs to validate — ownership is enforced in the handler via ICurrentUser.</summary>
internal sealed class GetAnalysisSessionsQueryValidator : AbstractValidator<GetAnalysisSessionsQuery>
{
    public GetAnalysisSessionsQueryValidator()
    {
    }
}
