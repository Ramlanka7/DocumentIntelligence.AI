using System.Linq.Expressions;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository abstraction for aggregate-root and entity access. Implemented in the
/// Persistence layer over EF Core; handlers depend on this interface, never on <c>DbContext</c>.
/// </summary>
/// <typeparam name="T">A domain entity that derives from <see cref="BaseEntity"/>.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Returns the entity with the given identifier, or <c>null</c> if not found.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all entities of this type, unfiltered.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all entities satisfying the given predicate.</summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an entity for insertion; persisted on the next <see cref="IUnitOfWork.SaveChangesAsync"/> call.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Marks an entity as modified; changes are persisted on the next <see cref="IUnitOfWork.SaveChangesAsync"/> call.</summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(T entity);

    /// <summary>Schedules an entity for deletion; persisted on the next <see cref="IUnitOfWork.SaveChangesAsync"/> call.</summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(T entity);
}
