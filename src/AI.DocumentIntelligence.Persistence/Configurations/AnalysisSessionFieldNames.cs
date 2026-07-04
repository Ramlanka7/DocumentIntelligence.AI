namespace AI.DocumentIntelligence.Persistence.Configurations;

/// <summary>
/// Compile-time constants for the private backing-field names mapped by
/// <see cref="AnalysisSessionConfiguration"/>. Using constants here keeps the
/// configuration and repositories in sync and avoids magic strings.
/// </summary>
internal static class AnalysisSessionFieldNames
{
    public const string DocumentIds = "_documentIds";
    public const string KeyFindings = "_keyFindings";
    public const string RisksIdentified = "_risksIdentified";
    public const string Recommendations = "_recommendations";
    public const string ActionItems = "_actionItems";
    public const string ReferencedSources = "_referencedSources";
}
