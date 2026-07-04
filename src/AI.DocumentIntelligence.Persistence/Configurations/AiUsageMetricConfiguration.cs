using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class AiUsageMetricConfiguration : IEntityTypeConfiguration<AiUsageMetric>
{
    public void Configure(EntityTypeBuilder<AiUsageMetric> builder)
    {
        builder.ToTable("ai_usage_metrics");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(m => m.SessionId)
            .HasColumnName("session_id");

        builder.Property(m => m.ProcessingTime)
            .HasColumnName("processing_time")
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

        builder.HasIndex(m => new { m.UserId, m.CreatedAtUtc })
            .HasDatabaseName("ix_ai_usage_metrics_user_id_created_at_utc");

        builder.Ignore(m => m.DomainEvents);
    }
}
