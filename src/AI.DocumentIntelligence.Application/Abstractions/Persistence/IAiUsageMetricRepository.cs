using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>Repository for <see cref="AiUsageMetric"/> ledger entries with admin-dashboard queries.</summary>
public interface IAiUsageMetricRepository : IRepository<AiUsageMetric>
{
    /// <summary>Returns all usage metric entries for the specified user.</summary>
    public Task<IReadOnlyList<AiUsageMetric>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate totals (prompt tokens, completion tokens, estimated cost, average
    /// processing time) for the optional user filter. Pass <see langword="null"/> to aggregate
    /// across all users (admin view).
    /// </summary>
    public Task<AiUsageTotals> GetTotalsAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}
