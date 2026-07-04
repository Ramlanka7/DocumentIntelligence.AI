using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>
/// Repository contract for the <see cref="User"/> aggregate. Extends the generic
/// <see cref="IRepository{T}"/> with email-based look-up needed by auth handlers.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Returns the user whose email matches <paramref name="email"/> (case-insensitive),
    /// or <see langword="null"/> when no such user exists.
    /// </summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Returns the user whose stored refresh-token hash matches <paramref name="tokenHash"/>,
    /// or <see langword="null"/> when no match exists or the token has been revoked.
    /// </summary>
    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);
}
