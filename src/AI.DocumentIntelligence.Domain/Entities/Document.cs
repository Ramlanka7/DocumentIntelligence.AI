using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// An uploaded source document. Owns its <see cref="FileMetadata"/> and the chunks produced
/// by the processing pipeline, and tracks its lifecycle <see cref="DocumentStatus"/>.
/// </summary>
public sealed class Document : AuditableEntity
{
    private readonly List<DocumentChunk> _chunks = [];

    private Document()
    {
        // EF / serialization constructor.
        Metadata = new FileMetadata(string.Empty, 0, 0, string.Empty);
        StoragePath = string.Empty;
    }

    private Document(Guid id, Guid ownerId, FileMetadata metadata, DocumentType type, string storagePath)
        : base(id)
    {
        OwnerId = ownerId;
        Metadata = metadata;
        Type = type;
        StoragePath = storagePath;
        Status = DocumentStatus.Pending;
    }

    public Guid OwnerId { get; private set; }

    public FileMetadata Metadata { get; private set; }

    public DocumentType Type { get; private set; }

    public DocumentStatus Status { get; private set; }

    public string StoragePath { get; private set; }

    public string? ExtractedText { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

    public static Document Create(Guid ownerId, FileMetadata metadata, DocumentType type, string storagePath) =>
        new(Guid.NewGuid(), ownerId, metadata, type, storagePath);

    public void MarkUploading() => Status = DocumentStatus.Uploading;

    public void MarkProcessing() => Status = DocumentStatus.Processing;

    public void MarkProcessed(string extractedText)
    {
        ExtractedText = extractedText;
        FailureReason = null;
        Status = DocumentStatus.Processed;
    }

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        Status = DocumentStatus.Failed;
    }

    public void AddChunk(DocumentChunk chunk) => _chunks.Add(chunk);

    public void ClearChunks() => _chunks.Clear();
}
