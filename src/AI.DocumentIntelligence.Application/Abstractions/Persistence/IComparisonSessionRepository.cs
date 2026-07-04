using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>Repository for <see cref="ComparisonSession"/> aggregates with session-specific queries.</summary>
public interface IComparisonSessionRepository : IRepository<ComparisonSession>
{
    /// <summary>Returns all comparison sessions owned by the specified user.</summary>
    public Task<IReadOnlyList<ComparisonSession>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
