using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;

/// <summary>Returns a summary list of the current user's analysis sessions, most-recent first.</summary>
public sealed record GetAnalysisSessionsQuery : IQuery<IReadOnlyList<AnalysisSessionSummaryDto>>;
