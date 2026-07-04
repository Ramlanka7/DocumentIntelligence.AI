using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.DocumentIntelligence.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(d => d.ExtractedText)
            .HasColumnName("extracted_text");

        builder.Property(d => d.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1024);

        builder.Property(d => d.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        // FileMetadata owned type: mapped as flat columns on the Documents table.
        builder.OwnsOne(d => d.Metadata, meta =>
        {
            meta.Property(m => m.FileName)
                .HasColumnName("metadata_file_name")
                .HasMaxLength(512)
                .IsRequired();

            meta.Property(m => m.SizeBytes)
                .HasColumnName("metadata_size_bytes")
                .IsRequired();

            meta.Property(m => m.PageCount)
                .HasColumnName("metadata_page_count")
                .IsRequired();

            meta.Property(m => m.ContentType)
                .HasColumnName("metadata_content_type")
                .HasMaxLength(128)
                .IsRequired();
        });

        // Chunks collection: one-to-many on public property, backed by _chunks field.
        builder.HasMany(d => d.Chunks)
            .WithOne()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Chunks)
            .HasField("_chunks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => d.OwnerId)
            .HasDatabaseName("ix_documents_owner_id");

        builder.Ignore(d => d.DomainEvents);
    }
}
