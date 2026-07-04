using AI.DocumentIntelligence.Application.Abstractions.Identity;

namespace AI.DocumentIntelligence.Infrastructure.Auth;

/// <summary>
/// BCrypt-based password hasher. Uses a work factor of 12 for a good balance of security
/// and performance. The Application layer depends only on <see cref="IPasswordHasher"/>.
/// </summary>
internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    /// <inheritdoc />
    public bool Verify(string plainPassword, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
}
