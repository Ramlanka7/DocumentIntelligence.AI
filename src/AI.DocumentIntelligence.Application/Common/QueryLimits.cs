namespace AI.DocumentIntelligence.Application.Common;

/// <summary>
/// Server-side caps for collection endpoints. No list endpoint may return an unbounded
/// result set — a growing table must degrade to "newest N" rather than an ever-larger
/// (and eventually memory-exhausting) response. Revisit when real pagination contracts
/// (page/pageSize + total count) are introduced.
/// </summary>
public static class QueryLimits
{
    /// <summary>Maximum items returned by any user-facing list endpoint.</summary>
    public const int MaxListResults = 500;
}
