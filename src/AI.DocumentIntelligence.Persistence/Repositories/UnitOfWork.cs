using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Persistence.Context;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Caches generic repositories per entity
/// type and delegates <see cref="SaveChangesAsync"/> to the underlying <see cref="AppDbContext"/>.
/// </summary>
internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    /// <inheritdoc />
    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            repo = new Repository<T>(context);
            _repositories[type] = repo;
        }

        return (IRepository<T>)repo;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
