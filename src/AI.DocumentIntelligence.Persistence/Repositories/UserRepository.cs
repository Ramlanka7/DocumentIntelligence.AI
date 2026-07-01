using System.Linq.Expressions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// Stub implementation of <see cref="IUserRepository"/>.
/// A full EF Core implementation will replace this when T02 (Persistence) is complete.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    // T02 is not yet done; all methods throw NotImplementedException with a clear message.

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public Task<IReadOnlyList<User>> FindAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public Task AddAsync(User entity, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public void Update(User entity) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public void Remove(User entity) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        throw new NotImplementedException("UserRepository is a stub pending T02 (EF Core Persistence).");
}
