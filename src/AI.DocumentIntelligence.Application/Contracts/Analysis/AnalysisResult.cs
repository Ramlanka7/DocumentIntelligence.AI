using AI.DocumentIntelligence.Application.Contracts;

namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>
/// The structured output of a document analysis, mirroring the spec's required result shape. Every
/// nested item and the top-level <see cref="Sources"/> carry citations.
/// </summary>
/// <param name="ExecutiveSummary">High-level summary of the analysed material.</param>
/// <param name="KeyFindings">The most important findings.</param>
/// <param name="Risks">Risks identified.</param>
/// <param name="Recommendations">Recommended actions.</param>
/// <param name="ActionItems">Concrete action items.</param>
/// <param name="Sources">The full set of citations referenced across the result.</param>
public sealed record AnalysisResult(
    string ExecutiveSummary,
    IReadOnlyList<KeyFinding> KeyFindings,
    IReadOnlyList<RiskItem> Risks,
    IReadOnlyList<Recommendation> Recommendations,
    IReadOnlyList<ActionItem> ActionItems,
    IReadOnlyList<Citation> Sources);
