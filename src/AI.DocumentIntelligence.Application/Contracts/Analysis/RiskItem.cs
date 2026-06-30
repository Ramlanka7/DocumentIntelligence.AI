using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>An identified risk with a severity rating and supporting citations.</summary>
/// <param name="Title">Short headline for the risk.</param>
/// <param name="Description">Explanation of the risk and its impact.</param>
/// <param name="Severity">Severity rating (e.g. "Low", "Medium", "High", "Critical").</param>
/// <param name="Citations">Sources supporting the risk.</param>
public sealed record RiskItem(
    string Title,
    string Description,
    string Severity,
    IReadOnlyList<Citation> Citations);
