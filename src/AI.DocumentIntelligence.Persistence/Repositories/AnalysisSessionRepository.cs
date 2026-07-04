using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.Configurations;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

internal sealed class AnalysisSessionRepository : Repository<AnalysisSession>, IAnalysisSessionRepository
{
    public AnalysisSessionRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AnalysisSession>> GetByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => EF.Property<List<Guid>>(s, AnalysisSessionFieldNames.DocumentIds).Contains(documentId))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
