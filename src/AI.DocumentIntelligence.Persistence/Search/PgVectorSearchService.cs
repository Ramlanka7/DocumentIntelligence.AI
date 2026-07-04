using System.Diagnostics;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Search;

/// <summary>
/// pgvector-backed implementation of <see cref="ISearchService"/>.
///
/// Uses <see cref="AppDbContext"/> and the HNSW cosine-distance index created by the
/// <c>InitialCreate</c> migration to store and retrieve <see cref="DocumentChunk"/> rows.
///
/// Activated when <c>AzureSearch:Endpoint</c> is absent or blank in configuration.
/// Registered as the <see cref="ISearchService"/> by
/// <see cref="AI.DocumentIntelligence.Persistence.DependencyInjection"/> in that case,
/// overriding the Azure implementation registered by Infrastructure.
///
/// Architecture note: <see cref="AppDbContext"/> must not leave the Persistence layer.
/// This class lives here deliberately so callers (Application / Infrastructure / Api)
/// never take a direct dependency on <see cref="AppDbContext"/>.
///
/// DI note: This service is registered as a Singleton. It takes an
/// <see cref="IServiceScopeFactory"/> to create a fresh <see cref="IServiceScope"/> — and
/// therefore a fresh scoped <see cref="AppDbContext"/> — for each operation, which is the
/// correct pattern for singleton services that need scoped EF Core contexts.
/// </summary>
internal sealed partial class PgVectorSearchService : ISearchService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<PgVectorSearchService> _logger;

    public PgVectorSearchService(
        IServiceScopeFactory scopeFactory,
        IEmbeddingService embeddingService,
        ILogger<PgVectorSearchService> logger)
    {
        _scopeFactory = scopeFactory;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Upserts document chunks into the <c>document_chunks</c> table using a
    /// delete-then-insert pattern keyed on <c>document_id</c>. This gives clean
    /// re-ingest semantics (old chunks are replaced atomically) without requiring a
    /// composite unique constraint.
    /// </remarks>
    public async Task<Result> IndexAsync(
        IReadOnlyList<SearchableChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return Result.Success();
        }

        var sw = Stopwatch.StartNew();

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Delete any existing chunks for the documents in this batch so re-ingest is clean.
            var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
            var deleted = await db.DocumentChunks
                .Where(dc => documentIds.Contains(dc.DocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                LogPreviousChunksDeleted(_logger, deleted);
            }

            // Build domain entities and persist.
            var entities = chunks.Select(c =>
            {
                var chunk = DocumentChunk.Create(
                    c.DocumentId,
                    c.ChunkIndex,
                    c.Content,
                    c.PageNumber,
                    c.ParagraphReference,
                    c.Embedding.Count);   // TokenCount ≈ embedding dimension for now

                chunk.SetEmbedding(c.Embedding);
                return chunk;
            }).ToList();

            await db.DocumentChunks.AddRangeAsync(entities, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            sw.Stop();
            LogIndexed(_logger, chunks.Count, sw.Elapsed.TotalMilliseconds);
            return Result.Success();
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogIndexFailed(_logger, ex);
            return Result.Failure(
                Error.Failure("PgVector.IndexFailed", $"pgvector indexing failed: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Executes a pgvector cosine nearest-neighbour query using the HNSW index.
    ///
    /// The EF LINQ expression:
    /// <code>
    ///   .OrderBy(c =&gt; c.Embedding!.CosineDistance(queryVector))
    ///   .Take(request.TopK)
    /// </code>
    /// translates to SQL:
    /// <code>
    ///   ORDER BY embedding &lt;=&gt; $1::vector LIMIT $2
    /// </code>
    ///
    /// <see cref="Pgvector.EntityFrameworkCore.VectorDbFunctionsExtensions"/> provides
    /// <c>CosineDistance</c> as an extension method on <c>Object</c>, so it can be called
    /// on the value-converted <c>IReadOnlyList&lt;float&gt;?</c> property and EF Core's
    /// Npgsql provider translates the expression tree to the <c>&lt;=&gt;</c> operator.
    ///
    /// The cosine distance returned is in [0, 2] (0 = identical, 2 = opposite);
    /// it is converted to a similarity score in [0, 1] via <c>score = 1 − distance / 2</c>.
    /// </remarks>
    public async Task<Result<IReadOnlyList<SearchHit>>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Generate query embedding using the configured embedding service (e.g. Azure OpenAI).
        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(
            request.Query, cancellationToken);

        if (embeddingResult.IsFailure)
        {
            sw.Stop();
            LogEmbeddingFailed(_logger, request.Query, embeddingResult.Error.Description);
            return Result.Failure<IReadOnlyList<SearchHit>>(embeddingResult.Error);
        }

        var queryVector = new Vector(embeddingResult.Value.ToArray());

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Build base query, filtered to requested documents when specified.
            IQueryable<DocumentChunk> baseQuery = db.DocumentChunks;

            if (request.DocumentIds.Count > 0)
            {
                var ids = request.DocumentIds.ToList();
                baseQuery = baseQuery.Where(c => ids.Contains(c.DocumentId));
            }

            // Cosine distance via pgvector's <=> operator.
            // VectorDbFunctionsExtensions.CosineDistance is an extension on Object,
            // so it resolves on the value-converted IReadOnlyList<float>? property and
            // the EF/Pgvector provider translates it to the native <=> SQL operator.
            var rawHits = await baseQuery
                .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
                .Take(request.TopK)
                .Select(c => new
                {
                    c.DocumentId,
                    c.Content,
                    c.PageNumber,
                    c.ParagraphReference,
                    Distance = c.Embedding!.CosineDistance(queryVector),
                })
                .ToListAsync(cancellationToken);

            // Resolve document names in one query (single round-trip).
            var docIds = rawHits.Select(h => h.DocumentId).Distinct().ToList();
            var docNames = await db.Documents
                .Where(d => docIds.Contains(d.Id))
                .Select(d => new { d.Id, Name = d.Metadata.FileName })
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

            // Build SearchHit list with citations-ready metadata.
            var hits = rawHits.Select(h =>
            {
                // score in [0, 1]: 1 = identical, 0 = maximally different
                var score = 1.0 - h.Distance / 2.0;
                var documentName = docNames.TryGetValue(h.DocumentId, out var name)
                    ? name
                    : h.DocumentId.ToString();

                return new SearchHit(
                    DocumentId: h.DocumentId,
                    DocumentName: documentName,
                    PageNumber: h.PageNumber,
                    ParagraphReference: h.ParagraphReference,
                    Content: h.Content,
                    Score: score);
            }).ToList();

            sw.Stop();
            LogSearched(_logger, request.Query, hits.Count, sw.Elapsed.TotalMilliseconds);
            return Result.Success<IReadOnlyList<SearchHit>>(hits);
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogSearchFailed(_logger, ex);
            return Result.Failure<IReadOnlyList<SearchHit>>(
                Error.Failure("PgVector.SearchFailed", $"pgvector search failed: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var count = await db.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ExecuteDeleteAsync(cancellationToken);

            LogDeleted(_logger, count, documentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogDeleteFailed(_logger, documentId, ex);
            return Result.Failure(
                Error.Failure("PgVector.DeleteFailed",
                    $"pgvector delete for document {documentId} failed: {ex.Message}"));
        }
    }

    // ---- Structured logger messages ----

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "pgvector: removed {Count} stale chunk(s) before re-index")]
    private static partial void LogPreviousChunksDeleted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "pgvector: indexed {Count} chunk(s) in {ElapsedMs:F1} ms")]
    private static partial void LogIndexed(ILogger logger, int count, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "pgvector: indexing failed")]
    private static partial void LogIndexFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "pgvector: embedding generation failed for query '{Query}': {Error}")]
    private static partial void LogEmbeddingFailed(ILogger logger, string query, string error);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "pgvector: query '{Query}' returned {Count} hit(s) in {ElapsedMs:F1} ms")]
    private static partial void LogSearched(ILogger logger, string query, int count, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "pgvector: search query failed")]
    private static partial void LogSearchFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "pgvector: deleted {Count} chunk(s) for document {DocumentId}")]
    private static partial void LogDeleted(ILogger logger, int count, Guid documentId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "pgvector: delete failed for document {DocumentId}")]
    private static partial void LogDeleteFailed(ILogger logger, Guid documentId, Exception exception);
}
