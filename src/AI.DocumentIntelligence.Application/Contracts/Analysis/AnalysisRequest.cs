namespace AI.DocumentIntelligence.Application.Contracts.Analysis;

/// <summary>A request to analyse one or more processed documents.</summary>
/// <param name="DocumentIds">The documents to analyse.</param>
/// <param name="Capability">The analysis capability to apply (e.g. "ExecutiveSummary", "RiskAssessment").</param>
/// <param name="CustomQuestion">An optional free-text question for custom question-answering.</param>
public sealed record AnalysisRequest(
    IReadOnlyList<Guid> DocumentIds,
    string Capability,
    string? CustomQuestion = null);
