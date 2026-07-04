using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Documents;

/// <summary>
/// Splits a <see cref="DocumentExtractionResult"/> into retrievable <see cref="DocumentChunk"/>s,
/// preserving citation metadata (page number, section reference) for every slice produced.
/// The implementation uses a configurable sliding-window strategy with optional section-awareness.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Produces an ordered list of <see cref="DocumentChunk"/>s for the given extraction output.
    /// </summary>
    /// <param name="documentId">The owning document identifier.</param>
    /// <param name="extractionResult">Structured output from the document processor (text, pages, sections).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Ordered chunks with page/section metadata, or a failure <see cref="Result"/>.</returns>
    public Task<Result<IReadOnlyList<DocumentChunk>>> ChunkAsync(
        Guid documentId,
        DocumentExtractionResult extractionResult,
        CancellationToken cancellationToken = default);
}
