using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Domain.ValueObjects;

/// <summary>
/// A single entry in a comparison's detailed change log, supporting the GitHub-style diff view.
/// </summary>
/// <param name="Status">Whether the content was added, removed, or modified.</param>
/// <param name="Section">The section, clause, or heading the change applies to.</param>
/// <param name="OldContent">The content in the earlier document, if any.</param>
/// <param name="NewContent">The content in the later document, if any.</param>
/// <param name="Description">A human-readable description of the change.</param>
public sealed record ChangeLogEntry(
    ChangeStatus Status,
    string Section,
    string? OldContent,
    string? NewContent,
    string Description);
