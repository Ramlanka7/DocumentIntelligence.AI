using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(u => u.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(512);

        builder.Property(u => u.RefreshTokenExpiresAtUtc)
            .HasColumnName("refresh_token_expires_at_utc");

        builder.Property(u => u.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Optimistic concurrency via PostgreSQL's xmin system column: concurrent updates to
        // the same user row (role change vs. deactivation vs. token rotation) now fail fast
        // with DbUpdateConcurrencyException instead of silently last-write-wins.
        builder.Property<uint>("xmin")
            .IsRowVersion();

        // Unique index on email; emails are stored normalized (lowercased) by the domain.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        // Ignore DomainEvents — they are transient and never persisted.
        builder.Ignore(u => u.DomainEvents);
    }
}
