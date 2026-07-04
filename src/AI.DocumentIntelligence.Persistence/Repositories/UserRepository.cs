using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>, extending the generic
/// <see cref="Repository{T}"/> with email- and refresh-token-based lookups.
/// </summary>
internal sealed class UserRepository(AppDbContext context)
    : Repository<User>(context), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalised = email.Trim().ToLowerInvariant();
        return await DbSet
            .FirstOrDefaultAsync(u => u.Email == normalised, ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await DbSet
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash, ct);
}
