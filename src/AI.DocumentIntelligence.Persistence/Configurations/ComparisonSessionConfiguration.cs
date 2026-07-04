using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class ComparisonSessionConfiguration : IEntityTypeConfiguration<ComparisonSession>
{
    public void Configure(EntityTypeBuilder<ComparisonSession> builder)
    {
        builder.ToTable("comparison_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(s => s.ComparisonType)
            .HasColumnName("comparison_type")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.ExecutiveOverview)
            .HasColumnName("executive_overview");

        builder.Property(s => s.RiskAnalysis)
            .HasColumnName("risk_analysis");

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

        // Primitive collection: List<Guid> stored as jsonb.
        builder.PrimitiveCollection<List<Guid>>("_documentIds")
            .HasField("_documentIds")
            .HasColumnName("document_ids")
            .HasColumnType("jsonb")
            .IsRequired();

        // Primitive collections: List<string> stored as jsonb.
        builder.PrimitiveCollection<List<string>>("_keyDifferences")
            .HasField("_keyDifferences")
            .HasColumnName("key_differences")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.PrimitiveCollection<List<string>>("_recommendations")
            .HasField("_recommendations")
            .HasColumnName("recommendations")
            .HasColumnType("jsonb")
            .IsRequired();

        // ChangeLogEntry collection: OwnsMany on the public property, accessing via backing field.
        builder.OwnsMany(s => s.DetailedChangeLog, entry =>
        {
            entry.ToJson("detailed_change_log");

            entry.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .HasConversion<string>();

            entry.Property(e => e.Section)
                .HasColumnName("section")
                .HasMaxLength(1024);

            entry.Property(e => e.OldContent)
                .HasColumnName("old_content");

            entry.Property(e => e.NewContent)
                .HasColumnName("new_content");

            entry.Property(e => e.Description)
                .HasColumnName("description");
        });

        builder.Navigation(s => s.DetailedChangeLog)
            .HasField("_detailedChangeLog")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Citation collection stored as a JSON column.
        builder.OwnsMany(s => s.SourceCitations, citation =>
        {
            citation.ToJson("source_citations");

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

        builder.Navigation(s => s.SourceCitations)
            .HasField("_sourceCitations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.OwnerId)
            .HasDatabaseName("ix_comparison_sessions_owner_id");

        builder.Ignore(s => s.DomainEvents);
    }
}
