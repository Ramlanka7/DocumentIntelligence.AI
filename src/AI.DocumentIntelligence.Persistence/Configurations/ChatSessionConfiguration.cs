using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Primitive collection: List<Guid> stored as jsonb via the private backing field.
        builder.PrimitiveCollection<List<Guid>>("_documentIds")
            .HasField("_documentIds")
            .HasColumnName("document_ids")
            .HasColumnType("jsonb")
            .IsRequired();

        // Messages collection: one-to-many via the public property, backed by _messages field.
        builder.HasMany(s => s.Messages)
            .WithOne()
            .HasForeignKey(m => m.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Messages)
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.OwnerId)
            .HasDatabaseName("ix_chat_sessions_owner_id");

        builder.Ignore(s => s.DomainEvents);
    }
}
