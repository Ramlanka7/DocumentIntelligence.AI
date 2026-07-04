namespace AI.DocumentIntelligence.Application.Features.Analysis.GetAnalysisSessions;

/// <summary>Summary view of an analysis session for history list endpoints.</summary>
/// <param name="Id">The session's unique identifier.</param>
/// <param name="Capability">The analysis capability used (e.g. ExecutiveSummary).</param>
/// <param name="DocumentIds">The document identifiers analysed in this session.</param>
/// <param name="Status">The session status (Pending | InProgress | Completed | Failed).</param>
/// <param name="ExecutiveSummary">A short executive summary, or null if not yet completed.</param>
/// <param name="CreatedAt">When the session was created (UTC).</param>
public sealed record AnalysisSessionSummaryDto(
    Guid Id,
    string Capability,
    IReadOnlyList<Guid> DocumentIds,
    string Status,
    string? ExecutiveSummary,
    DateTimeOffset CreatedAt);
