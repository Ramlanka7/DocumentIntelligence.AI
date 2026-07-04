using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

internal sealed class ComparisonSessionRepository : Repository<ComparisonSession>, IComparisonSessionRepository
{
    public ComparisonSessionRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<ComparisonSession>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(s => s.OwnerId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
