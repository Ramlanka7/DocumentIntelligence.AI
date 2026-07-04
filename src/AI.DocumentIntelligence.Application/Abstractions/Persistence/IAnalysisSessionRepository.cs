using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>Repository for <see cref="AnalysisSession"/> aggregates with session-specific queries.</summary>
public interface IAnalysisSessionRepository : IRepository<AnalysisSession>
{
    /// <summary>Returns all analysis sessions that reference the specified document.</summary>
    public Task<IReadOnlyList<AnalysisSession>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
