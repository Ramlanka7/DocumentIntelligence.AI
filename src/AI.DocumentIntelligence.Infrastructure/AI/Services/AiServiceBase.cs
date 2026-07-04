using System.Text.Json;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;
using DomainCitation = AI.DocumentIntelligence.Domain.ValueObjects.Citation;
using DomainTokenUsage = AI.DocumentIntelligence.Domain.ValueObjects.TokenUsage;

namespace AI.DocumentIntelligence.Infrastructure.AI.Services;

/// <summary>
/// Shared helpers for the three AI services: context retrieval, AI completion, JSON parsing,
/// citation mapping, and usage metric persistence (best-effort while T02 is a stub).
/// </summary>
internal abstract partial class AiServiceBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISearchService _searchService;
    private readonly ILogger _logger;

    /// <summary>The active AI provider (injected, selected by config).</summary>
    protected IAIProvider Provider { get; }

    /// <summary>The unit of work for persistence operations (shared with derived classes).</summary>
    protected IUnitOfWork UnitOfWork { get; }

    protected AiServiceBase(
        IAIProvider provider,
        ISearchService searchService,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        Provider = provider;
        _searchService = searchService;
        UnitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Searches the index for the most relevant chunks scoped to the given documents. Returns an
    /// empty list (not a failure) when nothing is retrieved, so callers always get a valid context.
    /// </summary>
    protected async Task<IReadOnlyList<SearchHit>> RetrieveContextAsync(
        string query,
        IReadOnlyList<Guid> documentIds,
        int topK,
        CancellationToken cancellationToken)
    {
        var searchRequest = new SearchRequest(query, documentIds, topK, UseHybrid: true);
        var result = await _searchService.SearchAsync(searchRequest, cancellationToken);
        return result.IsSuccess ? result.Value : [];
    }

    /// <summary>Sends a single-turn system+user prompt to the active provider.</summary>
    protected async Task<Result<AiCompletionResult>> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var messages = new List<AiMessage>
        {
            new(AiRole.System, systemPrompt),
            new(AiRole.User, userPrompt)
        };

        return await Provider.CompleteAsync(
            new AiCompletionRequest(messages, Temperature: 0.2),
            cancellationToken);
    }

    /// <summary>
    /// Strips optional markdown code-fence wrappers that models sometimes add, then deserialises
    /// the JSON into <typeparamref name="T"/>.
    /// </summary>
    protected static Result<T> ParseJson<T>(string rawContent)
    {
        var json = ExtractJsonContent(rawContent);

        try
        {
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (value is null)
            {
                return Result.Failure<T>(
                    Error.Failure("AI.NullResponse", "The AI provider returned a null JSON response."));
            }

            return Result.Success(value);
        }
        catch (JsonException ex)
        {
            return Result.Failure<T>(
                Error.Failure("AI.JsonParseFailed", $"Failed to parse AI JSON response: {ex.Message}"));
        }
    }

    /// <summary>
    /// Enqueues an <see cref="AiUsageMetric"/> into the current Unit of Work without committing.
    /// The caller is responsible for calling <see cref="IUnitOfWork.SaveChangesAsync"/> to persist
    /// both the session completion and the metric atomically. Failures are logged and swallowed.
    /// </summary>
    protected async Task EnlistUsageMetricAsync(
        Guid userId,
        string operationType,
        Application.Contracts.AI.TokenUsage contractUsage,
        TimeSpan processingTime,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var domainUsage = new DomainTokenUsage(
                contractUsage.PromptTokens,
                contractUsage.CompletionTokens,
                contractUsage.EstimatedCost);

            var metric = AiUsageMetric.Create(
                userId, operationType, domainUsage, processingTime, sessionId);

            await UnitOfWork.Repository<AiUsageMetric>().AddAsync(metric, cancellationToken);
        }
        catch (Exception ex)
        {
            LogUsageTrackingFailed(_logger, operationType, ex);
        }
    }

    // ---- Citation and contract mapping helpers ----

    protected static IReadOnlyList<Citation> MapCitations(IEnumerable<JsonCitationDto> dtos)
    {
        var citations = new List<Citation>();

        foreach (var dto in dtos)
        {
            if (!Guid.TryParse(dto.DocumentId, out var documentId))
            {
                continue;
            }

            var domainCitation = DomainCitation.Create(
                documentId,
                dto.DocumentName,
                dto.PageNumber,
                dto.ParagraphReference,
                dto.Snippet,
                dto.ConfidenceScore);

            if (domainCitation.IsSuccess)
            {
                var c = domainCitation.Value;
                citations.Add(new Citation(
                    c.DocumentId,
                    c.DocumentName,
                    c.PageNumber,
                    c.ParagraphReference,
                    c.Snippet,
                    c.ConfidenceScore));
            }
        }

        return citations;
    }

    protected static KeyFinding MapKeyFinding(JsonKeyFindingDto dto) =>
        new(dto.Title, dto.Detail, MapCitations(dto.Citations));

    protected static RiskItem MapRiskItem(JsonRiskItemDto dto) =>
        new(dto.Title, dto.Description, dto.Severity, MapCitations(dto.Citations));

    protected static Recommendation MapRecommendation(JsonRecommendationDto dto) =>
        new(dto.Title, dto.Detail, MapCitations(dto.Citations));

    protected static ActionItem MapActionItem(JsonActionItemDto dto) =>
        new(dto.Description, dto.Owner, MapCitations(dto.Citations));

    protected static DocumentDifference MapDifference(JsonDocumentDifferenceDto dto)
    {
        var differenceType = dto.Type.ToUpperInvariant() switch
        {
            "ADDED" => DifferenceType.Added,
            "REMOVED" => DifferenceType.Removed,
            _ => DifferenceType.Modified
        };

        return new DocumentDifference(
            differenceType,
            dto.Section,
            dto.Before,
            dto.After,
            dto.Summary,
            MapCitations(dto.Citations));
    }

    private static string ExtractJsonContent(string raw)
    {
        var trimmed = raw.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var start = trimmed.IndexOf('\n') + 1;
            var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start)
            {
                return trimmed[start..end].Trim();
            }
        }

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var start = trimmed.IndexOf('\n') + 1;
            var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start)
            {
                return trimmed[start..end].Trim();
            }
        }

        return trimmed;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "AI usage metric could not be persisted for operation '{OperationType}' — continuing without tracking")]
    private static partial void LogUsageTrackingFailed(
        ILogger logger, string operationType, Exception exception);
}
