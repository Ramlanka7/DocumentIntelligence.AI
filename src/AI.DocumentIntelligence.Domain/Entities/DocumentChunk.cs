using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// A retrievable slice of a <see cref="Document"/> with the location metadata needed to build
/// citations and an optional embedding vector for semantic search.
///
/// This is a transient pipeline type, not a persisted entity: chunks are produced during
/// ingestion and pushed to the search index, which is their system of record.
/// </summary>
public sealed class DocumentChunk : BaseEntity
{
    private DocumentChunk()
    {
        // EF / serialization constructor.
        Content = string.Empty;
        ParagraphReference = string.Empty;
    }

    private DocumentChunk(
        Guid id,
        Guid documentId,
        int index,
        string content,
        int pageNumber,
        string paragraphReference,
        int tokenCount)
        : base(id)
    {
        DocumentId = documentId;
        Index = index;
        Content = content;
        PageNumber = pageNumber;
        ParagraphReference = paragraphReference;
        TokenCount = tokenCount;
    }

    public Guid DocumentId { get; private set; }

    /// <summary>Zero-based ordinal of the chunk within its document.</summary>
    public int Index { get; private set; }

    public string Content { get; private set; }

    /// <summary>1-based source page the chunk was extracted from.</summary>
    public int PageNumber { get; private set; }

    public string ParagraphReference { get; private set; }

    public int TokenCount { get; private set; }

    /// <summary>Embedding vector for semantic search; null until embedded.</summary>
    public IReadOnlyList<float>? Embedding { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        int index,
        string content,
        int pageNumber,
        string paragraphReference,
        int tokenCount) =>
        new(Guid.NewGuid(), documentId, index, content, pageNumber, paragraphReference, tokenCount);

    public void SetEmbedding(IReadOnlyList<float> embedding) => Embedding = embedding;
}
