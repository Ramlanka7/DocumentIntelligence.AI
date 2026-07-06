using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDocumentRepository"/>, extending the generic
/// <see cref="Repository{T}"/> with owner-scoped retrieval.
/// </summary>
internal sealed class DocumentRepository(AppDbContext context)
    : Repository<Document>(context), IDocumentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Document>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await DbSet
            .AsNoTracking()
            .Where(d => d.OwnerId == ownerId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(QueryLimits.MaxListResults)
            .ToListAsync(ct);
}
