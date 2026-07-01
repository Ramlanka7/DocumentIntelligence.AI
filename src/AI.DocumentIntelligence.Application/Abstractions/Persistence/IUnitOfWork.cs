using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>
/// Coordinates multiple repository operations in a single transaction. Handlers acquire repositories
/// via <see cref="Repository{T}"/> and call <see cref="SaveChangesAsync"/> once at the end of their
/// use case — the Unit of Work pattern ensures atomicity without exposing <c>DbContext</c> to the
/// Application layer.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Returns the repository for the specified entity type.</summary>
    /// <typeparam name="T">A domain entity deriving from <see cref="BaseEntity"/>.</typeparam>
    public IRepository<T> Repository<T>() where T : BaseEntity;

    /// <summary>Commits all pending changes in the current transaction to the database.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
