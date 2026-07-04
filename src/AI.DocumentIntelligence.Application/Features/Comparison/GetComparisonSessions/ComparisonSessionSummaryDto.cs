namespace AI.DocumentIntelligence.Application.Features.Comparison.GetComparisonSessions;

/// <summary>Summary view of a comparison session for history list endpoints.</summary>
/// <param name="Id">The session's unique identifier.</param>
/// <param name="ComparisonType">The comparison type used (e.g. SideBySide, Contract).</param>
/// <param name="DocumentIds">The document identifiers compared in this session.</param>
/// <param name="Status">The session status (Pending | InProgress | Completed | Failed).</param>
/// <param name="ExecutiveOverview">A short executive overview, or null if not yet completed.</param>
/// <param name="CreatedAt">When the session was created (UTC).</param>
public sealed record ComparisonSessionSummaryDto(
    Guid Id,
    string ComparisonType,
    IReadOnlyList<Guid> DocumentIds,
    string Status,
    string? ExecutiveOverview,
    DateTimeOffset CreatedAt);
