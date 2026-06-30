using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>A recommended action arising from analysis, with supporting citations.</summary>
/// <param name="Title">Short headline for the recommendation.</param>
/// <param name="Detail">Explanation of what is recommended and why.</param>
/// <param name="Citations">Sources supporting the recommendation.</param>
public sealed record Recommendation(string Title, string Detail, IReadOnlyList<Citation> Citations);
