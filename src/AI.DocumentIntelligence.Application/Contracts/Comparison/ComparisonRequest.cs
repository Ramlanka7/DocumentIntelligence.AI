namespace AI.DocumentIntelligence.Application.Contracts.Comparison;

/// <summary>A request to compare two to four processed documents.</summary>
/// <param name="DocumentIds">The documents to compare (2–4).</param>
/// <param name="ComparisonType">The comparison type (e.g. "SideBySide", "Version", "Contract").</param>
/// <param name="CustomInstructions">Optional free-text instructions for a custom comparison.</param>
public sealed record ComparisonRequest(
    IReadOnlyList<Guid> DocumentIds,
    string ComparisonType,
    string? CustomInstructions = null);
