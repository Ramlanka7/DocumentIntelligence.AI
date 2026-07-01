using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// Stub implementation of <see cref="IUnitOfWork"/>.
/// A full EF Core implementation will replace this when T02 (Persistence) is complete.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : BaseEntity =>
        throw new NotImplementedException("UnitOfWork is a stub pending T02 (EF Core Persistence).");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UnitOfWork is a stub pending T02 (EF Core Persistence).");
}
