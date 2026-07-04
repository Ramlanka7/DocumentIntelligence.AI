using AI.DocumentIntelligence.Application.Abstractions.Persistence;

namespace AI.DocumentIntelligence.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Delegates to
/// <see cref="ApplicationDbContext.SaveChangesAsync"/> to flush all pending changes
/// in a single database round-trip.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
