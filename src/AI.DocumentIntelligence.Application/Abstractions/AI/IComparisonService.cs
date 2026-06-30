using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.AI;

/// <summary>
/// Compares multiple documents and produces a structured, citation-backed set of differences using
/// RAG retrieval and an <see cref="IAIProvider"/>.
/// </summary>
public interface IComparisonService
{
    /// <summary>Compares the requested documents.</summary>
    /// <param name="request">The comparison request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The structured comparison result with citations, or a failure <see cref="Result"/>.</returns>
    public Task<Result<ComparisonResult>> CompareAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default);
}
