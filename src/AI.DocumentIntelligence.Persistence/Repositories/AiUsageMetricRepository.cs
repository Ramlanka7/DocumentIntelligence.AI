using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

internal sealed class AiUsageMetricRepository : Repository<AiUsageMetric>, IAiUsageMetricRepository
{
    public AiUsageMetricRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AiUsageMetric>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<AiUsageTotals> GetTotalsAsync(
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(m => m.UserId == userId.Value);
        }

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPromptTokens = (long)g.Sum(m => m.TokenUsage.PromptTokens),
                TotalCompletionTokens = (long)g.Sum(m => m.TokenUsage.CompletionTokens),
                TotalEstimatedCost = g.Sum(m => m.TokenUsage.EstimatedCost),
                AverageProcessingTimeMs = g.Average(m => (double)m.ProcessingTime.TotalMilliseconds),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return totals is null
            ? new AiUsageTotals(0, 0, 0m, 0.0)
            : new AiUsageTotals(
                totals.TotalPromptTokens,
                totals.TotalCompletionTokens,
                totals.TotalEstimatedCost,
                totals.AverageProcessingTimeMs);
    }
}
