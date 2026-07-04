using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.ChatSessionId)
            .HasColumnName("chat_session_id")
            .IsRequired();

        builder.Property(m => m.Ordinal)
            .HasColumnName("ordinal")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // TokenUsage: complex property mapped as flat columns.
        builder.ComplexProperty(m => m.TokenUsage, tu =>
        {
            tu.Property(t => t.PromptTokens).HasColumnName("token_prompt_tokens");
            tu.Property(t => t.CompletionTokens).HasColumnName("token_completion_tokens");
            tu.Property(t => t.EstimatedCost)
                .HasColumnName("token_estimated_cost")
                .HasPrecision(18, 6);
        });

        // Citations: OwnsMany on the public property, accessing via the backing field.
        builder.OwnsMany(m => m.Citations, citation =>
        {
            citation.ToJson("citations");

            citation.Property(c => c.DocumentId).HasColumnName("document_id");
            citation.Property(c => c.DocumentName)
                .HasColumnName("document_name")
                .HasMaxLength(512);
            citation.Property(c => c.PageNumber).HasColumnName("page_number");
            citation.Property(c => c.ParagraphReference)
                .HasColumnName("paragraph_reference")
                .HasMaxLength(512);
            citation.Property(c => c.Snippet).HasColumnName("snippet");
            citation.Property(c => c.ConfidenceScore).HasColumnName("confidence_score");
        });

        builder.Navigation(m => m.Citations)
            .HasField("_citations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(m => m.ChatSessionId)
            .HasDatabaseName("ix_chat_messages_chat_session_id");

        builder.Ignore(m => m.DomainEvents);
    }
}
