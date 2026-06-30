using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>A concrete, actionable task extracted from analysis, with supporting citations.</summary>
/// <param name="Description">What needs to be done.</param>
/// <param name="Owner">Suggested owner/role responsible, if identified.</param>
/// <param name="Citations">Sources supporting the action item.</param>
public sealed record ActionItem(string Description, string? Owner, IReadOnlyList<Citation> Citations);
