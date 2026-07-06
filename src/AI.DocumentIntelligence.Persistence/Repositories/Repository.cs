using System.Linq.Expressions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository implementation that satisfies <see cref="IRepository{T}"/>.
/// All data access goes through <see cref="AppDbContext"/>; the context is never exposed outside
/// the Persistence layer.
/// </summary>
// CA1852 suppressed: this class is intentionally non-sealed because UserRepository and
// DocumentRepository derive from Repository<User>/Repository<Document> respectively.
// The Roslyn analyzer does not always detect generic-type subclassing within the same assembly.
#pragma warning disable CA1852
internal class Repository<T>(AppDbContext context) : IRepository<T>
#pragma warning restore CA1852
    where T : BaseEntity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindNewestAsync(
        Expression<Func<T, bool>> predicate,
        int maxResults,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(predicate)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public void Update(T entity) => DbSet.Update(entity);

    /// <inheritdoc />
    public void Remove(T entity) => DbSet.Remove(entity);
}
