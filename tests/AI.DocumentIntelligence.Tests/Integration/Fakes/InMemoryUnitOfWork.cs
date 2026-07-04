using System.Collections.Concurrent;
using System.Linq.Expressions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>
/// Functional in-memory unit-of-work for integration and unit tests. Repositories are backed by
/// per-type <see cref="ConcurrentDictionary{TKey,TValue}"/> so that <c>AddAsync</c> commits
/// immediately (no SaveChanges dance required) and <see cref="SaveChangesAsync"/> is a no-op.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    IRepository<T> IUnitOfWork.Repository<T>()
    {
        return (IRepository<T>)_repositories.GetOrAdd(
            typeof(T),
            _ => new GenericInMemoryRepository<T>());
    }

    /// <summary>
    /// Returns a typed handle to the in-memory repository for <typeparamref name="T"/> so that
    /// tests can inspect what was persisted.
    /// </summary>
    public GenericInMemoryRepository<T> GetRepository<T>() where T : BaseEntity =>
        (GenericInMemoryRepository<T>)((IUnitOfWork)this).Repository<T>();

    /// <summary>No-op — in-memory repositories persist immediately on Add.</summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(1);
}

/// <summary>
/// A simple in-memory <see cref="IRepository{T}"/> implementation backed by a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Writes are visible immediately so that tests
/// do not need to call <c>SaveChanges</c>.
/// </summary>
public sealed class GenericInMemoryRepository<T> : IRepository<T>
    where T : BaseEntity
{
    private readonly ConcurrentDictionary<Guid, T> _store = new();

    public IReadOnlyList<T> All => _store.Values.ToList().AsReadOnly();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var entity) ? entity : null);

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(_store.Values.ToList().AsReadOnly());

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(
            _store.Values.AsQueryable().Where(predicate).ToList().AsReadOnly());

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public void Update(T entity) =>
        _store[entity.Id] = entity;

    public void Remove(T entity) =>
        _store.TryRemove(entity.Id, out _);
}
