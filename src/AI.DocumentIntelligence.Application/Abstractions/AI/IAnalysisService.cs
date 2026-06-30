using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.AI;

/// <summary>
/// Produces a structured, citation-backed analysis of one or more documents using RAG retrieval and
/// an <see cref="IAIProvider"/>.
/// </summary>
public interface IAnalysisService
{
    /// <summary>Analyses the requested documents for the given capability.</summary>
    /// <param name="request">The analysis request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The structured analysis result with citations, or a failure <see cref="Result"/>.</returns>
    public Task<Result<AnalysisResult>> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default);
}
