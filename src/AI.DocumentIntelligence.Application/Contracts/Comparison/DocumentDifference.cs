using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Comparison;

/// <summary>A single difference between compared documents, suitable for a diff-style view.</summary>
/// <param name="Type">Whether the content was added, removed or modified.</param>
/// <param name="Section">The section/clause the difference relates to.</param>
/// <param name="Before">The prior text, or null when newly added.</param>
/// <param name="After">The new text, or null when removed.</param>
/// <param name="Summary">A human-readable description of the change.</param>
/// <param name="Citations">Sources supporting the difference.</param>
public sealed record DocumentDifference(
    DifferenceType Type,
    string Section,
    string? Before,
    string? After,
    string Summary,
    IReadOnlyList<Citation> Citations);
