using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>A platform user with a role that governs their access tier.</summary>
public sealed class User : AuditableEntity
{
    private User()
    {
        // EF / serialization constructor.
        Email = string.Empty;
        PasswordHash = string.Empty;
        FullName = string.Empty;
    }

    private User(Guid id, string email, string passwordHash, string fullName, UserRole role)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        IsActive = true;
    }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FullName { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public string? RefreshTokenHash { get; private set; }

    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; private set; }

    public static User Create(string email, string passwordHash, string fullName, UserRole role) =>
        new(Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash, fullName.Trim(), role);

    public void ChangeRole(UserRole role) => Role = role;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void SetRefreshToken(string tokenHash, DateTimeOffset expiresAtUtc)
    {
        RefreshTokenHash = tokenHash;
        RefreshTokenExpiresAtUtc = expiresAtUtc;
    }

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAtUtc = null;
    }
}
