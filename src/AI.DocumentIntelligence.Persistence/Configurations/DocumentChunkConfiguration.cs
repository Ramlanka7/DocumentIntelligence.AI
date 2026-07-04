using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    /// <summary>
    /// Embedding dimension matches Azure OpenAI text-embedding-3-small (and the AzureSearchOptions
    /// default of 1536). Must align with <c>AzureOpenAIOptions.EmbeddingDimensions</c> and
    /// <c>AzureSearchOptions.VectorDimensions</c> which both default to 1536.
    /// </summary>
    private const int EmbeddingDimension = 1536;

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(c => c.Index)
            .HasColumnName("index")
            .IsRequired();

        builder.Property(c => c.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(c => c.PageNumber)
            .HasColumnName("page_number")
            .IsRequired();

        builder.Property(c => c.ParagraphReference)
            .HasColumnName("paragraph_reference")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(c => c.TokenCount)
            .HasColumnName("token_count")
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // Map IReadOnlyList<float>? to pgvector Vector column using a value converter.
        builder.Property(c => c.Embedding)
            .HasColumnName("embedding")
            .HasColumnType($"vector({EmbeddingDimension})")
            .HasConversion(new EmbeddingVectorConverter());

        // HNSW index on the embedding column for fast approximate nearest-neighbour search.
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_document_chunks_embedding_hnsw");

        builder.HasIndex(c => c.DocumentId)
            .HasDatabaseName("ix_document_chunks_document_id");

        builder.Ignore(c => c.DomainEvents);
    }
}
