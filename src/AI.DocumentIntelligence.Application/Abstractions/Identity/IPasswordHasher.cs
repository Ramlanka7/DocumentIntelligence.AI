namespace AI.DocumentIntelligence.Application.Abstractions.Identity;

/// <summary>
/// Hashes and verifies passwords. Implemented in Infrastructure using BCrypt so the
/// Application layer stays free of cryptographic dependencies.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Returns a BCrypt hash of <paramref name="plainPassword"/>.</summary>
    public string Hash(string plainPassword);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="plainPassword"/> matches
    /// <paramref name="hashedPassword"/>.
    /// </summary>
    public bool Verify(string plainPassword, string hashedPassword);
}
