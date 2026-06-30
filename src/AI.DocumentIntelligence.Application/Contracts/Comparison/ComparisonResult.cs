using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;

namespace AI.DocumentIntelligence.Application.Contracts.Comparison;

/// <summary>
/// The structured output of a document comparison, mirroring the spec's required result shape.
/// The <see cref="Differences"/> drive the diff view; every section carries citations.
/// </summary>
/// <param name="ExecutiveOverview">High-level overview of how the documents differ.</param>
/// <param name="Differences">The detailed change log.</param>
/// <param name="Risks">Risk differences identified between the documents.</param>
/// <param name="Recommendations">Recommended actions arising from the comparison.</param>
/// <param name="Sources">The full set of citations referenced across the result.</param>
public sealed record ComparisonResult(
    string ExecutiveOverview,
    IReadOnlyList<DocumentDifference> Differences,
    IReadOnlyList<RiskItem> Risks,
    IReadOnlyList<Recommendation> Recommendations,
    IReadOnlyList<Citation> Sources);
