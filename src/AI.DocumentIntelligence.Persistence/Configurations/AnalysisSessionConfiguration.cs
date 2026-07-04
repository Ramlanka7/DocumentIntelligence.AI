using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class AnalysisSessionConfiguration : IEntityTypeConfiguration<AnalysisSession>
{
    public void Configure(EntityTypeBuilder<AnalysisSession> builder)
    {
        builder.ToTable("analysis_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(s => s.Capability)
            .HasColumnName("capability")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.CustomQuestion)
            .HasColumnName("custom_question");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.ExecutiveSummary)
            .HasColumnName("executive_summary");

        builder.Property(s => s.ProcessingTime)
            .HasColumnName("processing_time");

        builder.Property(s => s.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(2048);

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // TokenUsage: complex property mapped as flat columns.
        builder.ComplexProperty(s => s.TokenUsage, tu =>
        {
            tu.Property(t => t.PromptTokens).HasColumnName("token_prompt_tokens");
            tu.Property(t => t.CompletionTokens).HasColumnName("token_completion_tokens");
            tu.Property(t => t.EstimatedCost)
                .HasColumnName("token_estimated_cost")
                .HasPrecision(18, 6);
        });

        // Primitive collections: stored as jsonb.
        // The backing fields (_documentIds, _keyFindings, etc.) are private readonly List<T>.
        // EF Core 8+ supports primitive collections via PrimitiveCollection<T>("fieldName").
        builder.PrimitiveCollection<List<Guid>>("_documentIds")
            .HasField("_documentIds")
            .HasColumnName("document_ids")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.PrimitiveCollection<List<string>>("_keyFindings")
            .HasField("_keyFindings")
            .HasColumnName("key_findings")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.PrimitiveCollection<List<string>>("_risksIdentified")
            .HasField("_risksIdentified")
            .HasColumnName("risks_identified")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.PrimitiveCollection<List<string>>("_recommendations")
            .HasField("_recommendations")
            .HasColumnName("recommendations")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.PrimitiveCollection<List<string>>("_actionItems")
            .HasField("_actionItems")
            .HasColumnName("action_items")
            .HasColumnType("jsonb")
            .IsRequired();

        // Citation collection: OwnsMany on the public property, accessing via backing field.
        // Using the public property name avoids the EF "must be configured explicitly" error.
        builder.OwnsMany(s => s.ReferencedSources, citation =>
        {
            citation.ToJson("referenced_sources");

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

        builder.Navigation(s => s.ReferencedSources)
            .HasField("_referencedSources")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.OwnerId)
            .HasDatabaseName("ix_analysis_sessions_owner_id");

        builder.Ignore(s => s.DomainEvents);
    }
}
