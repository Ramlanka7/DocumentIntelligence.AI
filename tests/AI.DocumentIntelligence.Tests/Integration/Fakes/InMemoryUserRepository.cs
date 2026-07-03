using System.Linq.Expressions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>Thread-safe in-memory user store for integration tests.</summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.AsReadOnly());

    public Task<IReadOnlyList<User>> FindAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(
            _users.AsQueryable().Where(predicate).ToList().AsReadOnly());

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        _users.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(User entity)
    {
        // In-memory store: entity is already the live reference, no action needed.
    }

    public void Remove(User entity) => _users.Remove(entity);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.RefreshTokenHash == tokenHash));

    // ---- Test helpers ----

    /// <summary>Seeds the store with the given users before a test runs.</summary>
    public void Seed(IEnumerable<User> users) => _users.AddRange(users);

    /// <summary>Returns a snapshot of all users currently in the store.</summary>
    public IReadOnlyList<User> All => _users.AsReadOnly();
}
