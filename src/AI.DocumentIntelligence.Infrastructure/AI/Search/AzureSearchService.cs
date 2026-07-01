using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.AI.Search;

/// <summary>
/// Azure AI Search implementation of <see cref="ISearchService"/>. Creates/updates the index
/// schema on first use, upserts chunks, and supports vector-only, hybrid, and semantic retrieval.
/// All configuration is supplied via <see cref="AzureSearchOptions"/> (options pattern — no
/// hardcoded secrets or endpoints).
/// </summary>
internal sealed partial class AzureSearchService : ISearchService
{
    // ---- Field name constants (index schema) -----
    internal const string FieldId = "id";
    internal const string FieldDocumentId = "documentId";
    internal const string FieldDocumentName = "documentName";
    internal const string FieldChunkIndex = "chunkIndex";
    internal const string FieldContent = "content";
    internal const string FieldPageNumber = "pageNumber";
    internal const string FieldParagraphReference = "paragraphReference";
    internal const string FieldEmbedding = "embedding";

    private readonly AzureSearchOptions _options;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<AzureSearchService> _logger;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;

    private volatile bool _indexEnsured;

    public AzureSearchService(
        IOptions<AzureSearchOptions> options,
        IEmbeddingService embeddingService,
        ILogger<AzureSearchService> logger)
    {
        _options = options.Value;
        _embeddingService = embeddingService;
        _logger = logger;

        var credential = new AzureKeyCredential(_options.ApiKey);
        _indexClient = new SearchIndexClient(new Uri(_options.Endpoint), credential);
        _searchClient = new SearchClient(new Uri(_options.Endpoint), _options.IndexName, credential);
    }

    /// <inheritdoc />
    public async Task<Result> IndexAsync(
        IReadOnlyList<SearchableChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return Result.Success();
        }

        var ensureResult = await EnsureIndexExistsAsync(cancellationToken);
        if (ensureResult.IsFailure)
        {
            return ensureResult;
        }

        try
        {
            var documents = chunks.Select(ToSearchDocument).ToList();

            var batch = IndexDocumentsBatch.MergeOrUpload(documents);
            var response = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            var failed = response.Value.Results.Where(r => !r.Succeeded).ToList();
            if (failed.Count > 0)
            {
                var keys = string.Join(", ", failed.Select(r => r.Key));
                LogPartialIndexFailure(_logger, failed.Count, keys);
                return Result.Failure(
                    Error.Failure("Search.IndexPartialFailure",
                        $"Failed to index {failed.Count} chunk(s). Keys: {keys}"));
            }

            LogIndexed(_logger, chunks.Count, _options.IndexName);
            return Result.Success();
        }
        catch (RequestFailedException ex)
        {
            LogIndexRequestFailed(_logger, ex.Status, ex);
            return Result.Failure(
                Error.Failure("Search.IndexFailed", $"Azure Search indexing failed ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogIndexUnexpectedError(_logger, ex);
            return Result.Failure(
                Error.Failure("Search.IndexUnexpectedError", $"Unexpected indexing error: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SearchHit>>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var ensureResult = await EnsureIndexExistsAsync(cancellationToken);
        if (ensureResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SearchHit>>(ensureResult.Error);
        }

        // Generate query embedding for vector search.
        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        if (embeddingResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SearchHit>>(embeddingResult.Error);
        }

        var queryVector = embeddingResult.Value;

        try
        {
            var searchOptions = BuildSearchOptions(request, queryVector);
            var response = await _searchClient.SearchAsync<SearchDocument>(
                request.UseHybrid ? request.Query : "*",
                searchOptions,
                cancellationToken);

            var hits = new List<SearchHit>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var hit = MapToSearchHit(result.Document, result.Score);
                hits.Add(hit);
            }

            LogSearchResult(_logger, request.Query, hits.Count, request.UseHybrid);

            return Result.Success<IReadOnlyList<SearchHit>>(hits);
        }
        catch (RequestFailedException ex)
        {
            LogSearchRequestFailed(_logger, ex.Status, ex);
            return Result.Failure<IReadOnlyList<SearchHit>>(
                Error.Failure("Search.QueryFailed", $"Azure Search query failed ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogSearchUnexpectedError(_logger, ex);
            return Result.Failure<IReadOnlyList<SearchHit>>(
                Error.Failure("Search.QueryUnexpectedError", $"Unexpected search error: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var ensureResult = await EnsureIndexExistsAsync(cancellationToken);
        if (ensureResult.IsFailure)
        {
            return ensureResult;
        }

        try
        {
            var filter = $"{FieldDocumentId} eq '{documentId}'";
            var totalDeleted = 0;

            // Page through all matching chunks; a document may have more than MaxDeleteBatchSize chunks.
            while (true)
            {
                var searchOptions = new SearchOptions
                {
                    Filter = filter,
                    Select = { FieldId },
                    Size = _options.MaxDeleteBatchSize,
                };

                var response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions, cancellationToken);

                var ids = new List<string>();
                await foreach (var result in response.Value.GetResultsAsync())
                {
                    if (result.Document.TryGetValue(FieldId, out var idObj) && idObj is string id)
                    {
                        ids.Add(id);
                    }
                }

                if (ids.Count == 0)
                {
                    break;
                }

                var docsToDelete = ids.Select(id =>
                {
                    var doc = new SearchDocument();
                    doc[FieldId] = id;
                    return doc;
                }).ToList();

                var batch = IndexDocumentsBatch.Delete(docsToDelete);
                await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
                totalDeleted += ids.Count;

                // If the page was smaller than the batch size there are no more pages.
                if (ids.Count < _options.MaxDeleteBatchSize)
                {
                    break;
                }
            }

            LogDeleted(_logger, totalDeleted, documentId, _options.IndexName);

            return Result.Success();
        }
        catch (RequestFailedException ex)
        {
            LogDeleteRequestFailed(_logger, ex.Status, ex);
            return Result.Failure(
                Error.Failure("Search.DeleteFailed", $"Azure Search delete failed ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogDeleteUnexpectedError(_logger, ex);
            return Result.Failure(
                Error.Failure("Search.DeleteUnexpectedError", $"Unexpected delete error: {ex.Message}"));
        }
    }

    // ---- Private helpers ----

    /// <summary>
    /// Ensures the Azure AI Search index exists with the correct schema, creating or updating
    /// it as needed. Result is cached after the first successful call.
    /// </summary>
    private async Task<Result> EnsureIndexExistsAsync(CancellationToken cancellationToken)
    {
        if (_indexEnsured)
        {
            return Result.Success();
        }

        try
        {
            var definition = BuildIndexDefinition();
            await _indexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
            _indexEnsured = true;
            LogIndexEnsured(_logger, _options.IndexName);
            return Result.Success();
        }
        catch (RequestFailedException ex)
        {
            LogIndexCreationFailed(_logger, _options.IndexName, ex.Status, ex);
            return Result.Failure(
                Error.Failure("Search.IndexCreationFailed",
                    $"Failed to ensure search index '{_options.IndexName}' ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogIndexCreationUnexpectedError(_logger, _options.IndexName, ex);
            return Result.Failure(
                Error.Failure("Search.IndexCreationUnexpectedError",
                    $"Unexpected error ensuring search index: {ex.Message}"));
        }
    }

    /// <summary>Constructs the index schema with vector field and citation metadata fields.</summary>
    private SearchIndex BuildIndexDefinition()
    {
        var index = new SearchIndex(_options.IndexName)
        {
            Fields =
            {
                new SimpleField(FieldId, SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsFilterable = true,
                },
                new SimpleField(FieldDocumentId, SearchFieldDataType.String)
                {
                    IsFilterable = true,
                    IsFacetable = false,
                },
                new SearchableField(FieldDocumentName)
                {
                    IsFilterable = true,
                },
                new SimpleField(FieldChunkIndex, SearchFieldDataType.Int32)
                {
                    IsFilterable = false,
                    IsSortable = true,
                },
                new SearchableField(FieldContent)
                {
                    IsFilterable = false,
                },
                new SimpleField(FieldPageNumber, SearchFieldDataType.Int32)
                {
                    IsFilterable = true,
                    IsSortable = true,
                },
                new SearchableField(FieldParagraphReference)
                {
                    IsFilterable = true,
                },
                new SearchField(FieldEmbedding, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _options.VectorDimensions,
                    VectorSearchProfileName = "hnsw-profile",
                },
            },
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-algo")
                    {
                        Parameters = new HnswParameters
                        {
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500,
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                        },
                    },
                },
                Profiles =
                {
                    new VectorSearchProfile("hnsw-profile", "hnsw-algo"),
                },
            },
        };

        // Add semantic configuration when a name is specified.
        if (!string.IsNullOrWhiteSpace(_options.SemanticConfigurationName))
        {
            index.SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration(
                        _options.SemanticConfigurationName,
                        new SemanticPrioritizedFields
                        {
                            ContentFields = { new SemanticField(FieldContent) },
                            KeywordsFields = { new SemanticField(FieldDocumentName) },
                        }),
                },
            };
        }

        return index;
    }

    /// <summary>Builds the search options, applying vector, hybrid and optional semantic configuration.</summary>
    private SearchOptions BuildSearchOptions(SearchRequest request, IReadOnlyList<float> queryVector)
    {
        var vectorQuery = new VectorizedQuery(queryVector.ToArray())
        {
            KNearestNeighborsCount = request.TopK,
            Fields = { FieldEmbedding },
        };

        var options = new SearchOptions
        {
            Size = request.TopK,
            VectorSearch = new VectorSearchOptions
            {
                Queries = { vectorQuery },
            },
            Select =
            {
                FieldDocumentId,
                FieldDocumentName,
                FieldChunkIndex,
                FieldContent,
                FieldPageNumber,
                FieldParagraphReference,
            },
        };

        // Apply document-level filter when document IDs are specified.
        if (request.DocumentIds.Count > 0)
        {
            var ids = string.Join(" or ", request.DocumentIds.Select(id => $"{FieldDocumentId} eq '{id}'"));
            options.Filter = $"({ids})";
        }

        // Semantic re-ranking requires a textual query — only apply it for hybrid searches.
        if (request.UseHybrid && !string.IsNullOrWhiteSpace(_options.SemanticConfigurationName))
        {
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _options.SemanticConfigurationName,
            };
        }

        return options;
    }

    /// <summary>Projects a <see cref="SearchableChunk"/> into the flat Azure Search document format.</summary>
    private static SearchDocument ToSearchDocument(SearchableChunk chunk)
    {
        var doc = new SearchDocument
        {
            [FieldId] = $"{chunk.DocumentId}-{chunk.ChunkIndex}",
            [FieldDocumentId] = chunk.DocumentId.ToString(),
            [FieldDocumentName] = chunk.DocumentName,
            [FieldChunkIndex] = chunk.ChunkIndex,
            [FieldContent] = chunk.Content,
            [FieldPageNumber] = chunk.PageNumber,
            [FieldParagraphReference] = chunk.ParagraphReference,
            [FieldEmbedding] = chunk.Embedding.ToArray(),
        };
        return doc;
    }

    /// <summary>Maps a flat <see cref="SearchDocument"/> and score back to a <see cref="SearchHit"/>.</summary>
    private static SearchHit MapToSearchHit(SearchDocument doc, double? score)
    {
        var documentId = doc.TryGetValue(FieldDocumentId, out var dIdObj) && dIdObj is string dIdStr
            ? Guid.TryParse(dIdStr, out var parsed) ? parsed : Guid.Empty
            : Guid.Empty;

        var documentName = doc.TryGetValue(FieldDocumentName, out var nameObj) && nameObj is string nameStr
            ? nameStr
            : string.Empty;

        var pageNumber = doc.TryGetValue(FieldPageNumber, out var pageObj) && pageObj is int pageInt
            ? pageInt
            : 1;

        var paragraphRef = doc.TryGetValue(FieldParagraphReference, out var paraObj) && paraObj is string paraStr
            ? paraStr
            : string.Empty;

        var content = doc.TryGetValue(FieldContent, out var contentObj) && contentObj is string contentStr
            ? contentStr
            : string.Empty;

        return new SearchHit(
            DocumentId: documentId,
            DocumentName: documentName,
            PageNumber: pageNumber,
            ParagraphReference: paragraphRef,
            Content: content,
            Score: score ?? 0.0);
    }

    // ---- Logger messages ----

    [LoggerMessage(Level = LogLevel.Information, Message = "Azure AI Search index '{IndexName}' ensured")]
    private static partial void LogIndexEnsured(ILogger logger, string indexName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create/update Azure AI Search index '{IndexName}': {Status}")]
    private static partial void LogIndexCreationFailed(ILogger logger, string indexName, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error ensuring search index '{IndexName}'")]
    private static partial void LogIndexCreationUnexpectedError(ILogger logger, string indexName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to index {Count} chunk(s): {Keys}")]
    private static partial void LogPartialIndexFailure(ILogger logger, int count, string keys);

    [LoggerMessage(Level = LogLevel.Information, Message = "Indexed {Count} chunk(s) into '{IndexName}'")]
    private static partial void LogIndexed(ILogger logger, int count, string indexName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Azure Search index request failed: {Status}")]
    private static partial void LogIndexRequestFailed(ILogger logger, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during indexing")]
    private static partial void LogIndexUnexpectedError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search for '{Query}' returned {Count} hit(s) (hybrid={Hybrid})")]
    private static partial void LogSearchResult(ILogger logger, string query, int count, bool hybrid);

    [LoggerMessage(Level = LogLevel.Error, Message = "Azure Search query failed: {Status}")]
    private static partial void LogSearchRequestFailed(ILogger logger, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during search")]
    private static partial void LogSearchUnexpectedError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} chunk(s) for document {DocumentId} from '{IndexName}'")]
    private static partial void LogDeleted(ILogger logger, int count, Guid documentId, string indexName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Azure Search delete request failed: {Status}")]
    private static partial void LogDeleteRequestFailed(ILogger logger, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during delete")]
    private static partial void LogDeleteUnexpectedError(ILogger logger, Exception exception);
}
