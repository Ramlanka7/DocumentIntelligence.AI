using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Application.Features.RAG.Ingest;

/// <summary>
/// Handles <see cref="IngestDocumentCommand"/>: chunk → embed → index.
/// Returns a successful <see cref="Result"/> when all chunks are embedded and indexed.
/// </summary>
public sealed partial class IngestDocumentCommandHandler(
    IChunkingService chunkingService,
    IEmbeddingService embeddingService,
    ISearchService searchService,
    ILogger<IngestDocumentCommandHandler> logger)
    : ICommandHandler<IngestDocumentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Chunk the extracted document content.
        var chunkResult = await chunkingService.ChunkAsync(
            request.DocumentId,
            request.ExtractionResult,
            cancellationToken);

        if (chunkResult.IsFailure)
        {
            LogChunkingFailed(logger, request.DocumentId, chunkResult.Error.Description);
            return Result.Failure(chunkResult.Error);
        }

        var chunks = chunkResult.Value;

        if (chunks.Count == 0)
        {
            LogZeroChunks(logger, request.DocumentId);
            return Result.Success();
        }

        // Step 2: Generate embeddings for all chunks (batch call).
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddingResult = await embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

        if (embeddingResult.IsFailure)
        {
            LogEmbeddingFailed(logger, request.DocumentId, embeddingResult.Error.Description);
            return Result.Failure(embeddingResult.Error);
        }

        var embeddings = embeddingResult.Value;

        if (embeddings.Count != chunks.Count)
        {
            return Result.Failure(Error.Failure(
                "Ingest.EmbeddingCountMismatch",
                $"Expected {chunks.Count} embeddings but received {embeddings.Count}."));
        }

        // Step 3: Attach embeddings to chunks and build searchable chunk DTOs.
        var searchableChunks = new List<SearchableChunk>(chunks.Count);
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].SetEmbedding(embeddings[i]);

            searchableChunks.Add(new SearchableChunk(
                DocumentId: request.DocumentId,
                DocumentName: request.DocumentName,
                ChunkIndex: chunks[i].Index,
                Content: chunks[i].Content,
                PageNumber: chunks[i].PageNumber,
                ParagraphReference: chunks[i].ParagraphReference,
                Embedding: embeddings[i]));
        }

        // Step 4: Upsert all chunks into the search index.
        var indexResult = await searchService.IndexAsync(searchableChunks, cancellationToken);

        if (indexResult.IsFailure)
        {
            LogIndexingFailed(logger, request.DocumentId, indexResult.Error.Description);
            return Result.Failure(indexResult.Error);
        }

        LogIngested(logger, request.DocumentId, chunks.Count);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Chunking failed for document {DocumentId}: {Error}")]
    private static partial void LogChunkingFailed(ILogger logger, Guid documentId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Document {DocumentId} produced zero chunks — skipping embedding and indexing.")]
    private static partial void LogZeroChunks(ILogger logger, Guid documentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Embedding failed for document {DocumentId}: {Error}")]
    private static partial void LogEmbeddingFailed(ILogger logger, Guid documentId, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Indexing failed for document {DocumentId}: {Error}")]
    private static partial void LogIndexingFailed(ILogger logger, Guid documentId, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} ingested: {ChunkCount} chunk(s) embedded and indexed.")]
    private static partial void LogIngested(ILogger logger, Guid documentId, int chunkCount);
}
