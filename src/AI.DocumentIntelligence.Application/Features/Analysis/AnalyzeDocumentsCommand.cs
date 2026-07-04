using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Analysis;

namespace AI.DocumentIntelligence.Application.Features.Analysis;

/// <summary>
/// Triggers an AI-powered analysis of one or more documents for a given capability
/// (e.g. executive summary, risk assessment, custom question-answering).
/// </summary>
/// <param name="DocumentIds">The documents to analyse (1–4).</param>
/// <param name="Capability">The analysis capability to apply.</param>
/// <param name="CustomQuestion">An optional free-text question for custom QA.</param>
public sealed record AnalyzeDocumentsCommand(
    IReadOnlyList<Guid> DocumentIds,
    string Capability,
    string? CustomQuestion) : ICommand<AnalysisResult>;
