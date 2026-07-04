using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Application.Features.RAG.Search;

/// <summary>
/// Handles <see cref="SearchDocumentsQuery"/>: delegates to <see cref="ISearchService"/> and maps
/// each <see cref="SearchHit"/> to a <see cref="RetrievedChunk"/> and a <see cref="Citation"/>.
/// </summary>
public sealed partial class SearchDocumentsQueryHandler(
    ISearchService searchService,
    ILogger<SearchDocumentsQueryHandler> logger)
    : IQueryHandler<SearchDocumentsQuery, SearchDocumentsResponse>
{
    /// <inheritdoc />
    public async Task<Result<SearchDocumentsResponse>> Handle(
        SearchDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var searchRequest = new SearchRequest(
            Query: request.Query,
            DocumentIds: request.DocumentIds,
            TopK: request.TopK,
            UseHybrid: request.UseHybrid);

        var searchResult = await searchService.SearchAsync(searchRequest, cancellationToken);

        if (searchResult.IsFailure)
        {
            LogSearchFailed(logger, request.Query, searchResult.Error.Description);
            return Result.Failure<SearchDocumentsResponse>(searchResult.Error);
        }

        var hits = searchResult.Value;
        var chunks = new List<RetrievedChunk>(hits.Count);
        var citations = new List<Citation>(hits.Count);

        foreach (var hit in hits)
        {
            chunks.Add(new RetrievedChunk(
                DocumentId: hit.DocumentId,
                DocumentName: hit.DocumentName,
                PageNumber: hit.PageNumber,
                ParagraphReference: hit.ParagraphReference,
                Content: hit.Content,
                RelevanceScore: hit.Score));

            // Map score (0..n) to confidence (0..1): clamp to [0,1].
            var confidence = Math.Min(1.0, Math.Max(0.0, hit.Score));

            // Construct citation — skip if domain validation fails (e.g. page < 1).
            var citationResult = Domain.ValueObjects.Citation.Create(
                documentId: hit.DocumentId,
                documentName: hit.DocumentName,
                pageNumber: Math.Max(1, hit.PageNumber),
                paragraphReference: hit.ParagraphReference,
                snippet: TruncateSnippet(hit.Content),
                confidenceScore: confidence);

            if (citationResult.IsSuccess)
            {
                var domainCitation = citationResult.Value;
                citations.Add(new Citation(
                    DocumentId: domainCitation.DocumentId,
                    DocumentName: domainCitation.DocumentName,
                    PageNumber: domainCitation.PageNumber,
                    ParagraphReference: domainCitation.ParagraphReference,
                    Snippet: domainCitation.Snippet,
                    ConfidenceScore: domainCitation.ConfidenceScore));
            }
        }

        LogSearchResolved(logger, request.Query, chunks.Count, citations.Count);

        return Result.Success(new SearchDocumentsResponse(chunks, citations));
    }

    private static string TruncateSnippet(string content, int maxLength = 300)
    {
        if (content.Length <= maxLength)
        {
            return content;
        }

        return string.Concat(content.AsSpan(0, maxLength), "…");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Search failed for query '{Query}': {Error}")]
    private static partial void LogSearchFailed(ILogger logger, string query, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search query '{Query}' resolved {ChunkCount} chunk(s) with {CitationCount} citation(s).")]
    private static partial void LogSearchResolved(ILogger logger, string query, int chunkCount, int citationCount);
}
