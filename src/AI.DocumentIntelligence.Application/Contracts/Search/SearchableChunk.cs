namespace AI.DocumentIntelligence.Application.Contracts.Search;

/// <summary>A document chunk plus its embedding, ready to be written to the search index.</summary>
/// <param name="DocumentId">The owning document.</param>
/// <param name="DocumentName">Human-readable document name.</param>
/// <param name="ChunkIndex">Ordinal position of the chunk within the document.</param>
/// <param name="Content">The chunk text.</param>
/// <param name="PageNumber">1-based source page.</param>
/// <param name="ParagraphReference">Paragraph/section locator within the page.</param>
/// <param name="Embedding">The chunk's embedding vector.</param>
public sealed record SearchableChunk(
    Guid DocumentId,
    string DocumentName,
    int ChunkIndex,
    string Content,
    int PageNumber,
    string ParagraphReference,
    IReadOnlyList<float> Embedding);
