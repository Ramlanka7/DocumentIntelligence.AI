using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;

/// <summary>Returns a summary list of the current user's comparison sessions, most-recent first.</summary>
public sealed record GetComparisonSessionsQuery : IQuery<IReadOnlyList<ComparisonSessionSummaryDto>>;
