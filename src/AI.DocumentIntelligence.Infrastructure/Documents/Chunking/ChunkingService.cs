using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.Documents.Chunking;

/// <summary>
/// Splits a document extraction result into overlapping chunks using a configurable sliding-window
/// strategy. When <see cref="ChunkingOptions.SectionAware"/> is enabled the chunker respects
/// detected section boundaries so that no single chunk spans two headings.
/// </summary>
internal sealed partial class ChunkingService : IChunkingService
{
    private readonly ChunkingOptions _options;
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(IOptions<ChunkingOptions> options, ILogger<ChunkingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<DocumentChunk>>> ChunkAsync(
        Guid documentId,
        DocumentExtractionResult extractionResult,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chunks = _options.SectionAware && extractionResult.Sections.Count > 0
                ? ChunkBySections(documentId, extractionResult)
                : ChunkByPages(documentId, extractionResult);

            LogChunked(
                _logger,
                documentId,
                chunks.Count,
                _options.SectionAware);

            return Task.FromResult(Result.Success<IReadOnlyList<DocumentChunk>>(chunks));
        }
        catch (Exception ex)
        {
            LogChunkingFailed(_logger, documentId, ex);
            return Task.FromResult(
                Result.Failure<IReadOnlyList<DocumentChunk>>(
                    Error.Failure("Chunking.Failed", $"Document chunking failed: {ex.Message}")));
        }
    }

    /// <summary>
    /// Chunks by first splitting on sections, then applying the sliding window inside each section.
    /// This ensures chunks do not cross heading/section boundaries.
    /// </summary>
    private List<DocumentChunk> ChunkBySections(Guid documentId, DocumentExtractionResult extraction)
    {
        var allChunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        foreach (var section in extraction.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content))
            {
                continue;
            }

            var sectionText = $"{section.Heading}\n{section.Content}";
            var slices = SlidingWindow(sectionText);

            foreach (var slice in slices)
            {
                if (slice.Length < _options.MinChunkSize)
                {
                    continue;
                }

                var chunk = DocumentChunk.Create(
                    documentId,
                    chunkIndex,
                    slice,
                    section.StartPage,
                    section.Heading,
                    EstimateTokenCount(slice));

                allChunks.Add(chunk);
                chunkIndex++;
            }
        }

        // If sections produced nothing (e.g. all sections were too short), fall back to page-based.
        if (allChunks.Count == 0)
        {
            return ChunkByPages(documentId, extraction);
        }

        return allChunks;
    }

    /// <summary>
    /// Chunks page-by-page, using the page text as the unit before applying the sliding window.
    /// Page number and a generic paragraph reference ("p{page}-¶{n}") are preserved in every chunk.
    /// </summary>
    private List<DocumentChunk> ChunkByPages(Guid documentId, DocumentExtractionResult extraction)
    {
        var allChunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        // Prefer page-level text; fall back to full-text on a single virtual "page 1".
        var pages = extraction.Pages.Count > 0
            ? extraction.Pages
            : [new ExtractedPage(1, extraction.FullText)];

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
            {
                continue;
            }

            var slices = SlidingWindow(page.Text);
            var paraIndex = 1;

            foreach (var slice in slices)
            {
                if (slice.Length < _options.MinChunkSize)
                {
                    continue;
                }

                var chunk = DocumentChunk.Create(
                    documentId,
                    chunkIndex,
                    slice,
                    page.PageNumber,
                    $"p{page.PageNumber}-¶{paraIndex}",
                    EstimateTokenCount(slice));

                allChunks.Add(chunk);
                chunkIndex++;
                paraIndex++;
            }
        }

        return allChunks;
    }

    /// <summary>Applies the sliding window to a text block, producing overlapping slices.</summary>
    private IEnumerable<string> SlidingWindow(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var size = _options.ChunkSize;
        var overlap = Math.Min(_options.ChunkOverlap, size - 1);
        var step = size - overlap;

        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(size, text.Length - start);
            yield return text.Substring(start, length);
            start += step;
        }
    }

    /// <summary>
    /// Rough token-count estimate: GPT-family models average ~4 characters per token.
    /// Actual tokenisation is not needed here — the value is stored for informational purposes.
    /// </summary>
    private static int EstimateTokenCount(string text) => Math.Max(1, text.Length / 4);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} chunked into {ChunkCount} chunks (section-aware={SectionAware})")]
    private static partial void LogChunked(ILogger logger, Guid documentId, int chunkCount, bool sectionAware);

    [LoggerMessage(Level = LogLevel.Error, Message = "Chunking failed for document {DocumentId}")]
    private static partial void LogChunkingFailed(ILogger logger, Guid documentId, Exception exception);
}
