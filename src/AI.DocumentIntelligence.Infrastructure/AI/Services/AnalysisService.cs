using System.Diagnostics;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Infrastructure.AI.Prompts;
using Microsoft.Extensions.Logging;
using DomainCitation = AI.DocumentIntelligence.Domain.ValueObjects.Citation;
using DomainTokenUsage = AI.DocumentIntelligence.Domain.ValueObjects.TokenUsage;

namespace AI.DocumentIntelligence.Infrastructure.AI.Services;

/// <summary>
/// Implements <see cref="IAnalysisService"/> using RAG retrieval from <see cref="ISearchService"/>
/// and the configured <see cref="IAIProvider"/>. Results always carry citations.
/// Each call persists an <see cref="AnalysisSession"/> with full citation and token usage data.
/// </summary>
internal sealed partial class AnalysisService : AiServiceBase, IAnalysisService
{
    private const string OperationType = "Analysis";
    private const int ContextChunks = 10;

    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        IAIProvider provider,
        ISearchService searchService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AnalysisService> logger)
        : base(provider, searchService, unitOfWork, logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AnalysisResult>> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DocumentIds.Count == 0)
        {
            return Result.Failure<AnalysisResult>(Domain.Errors.DomainErrors.Analysis.NoDocuments);
        }

        if (request.DocumentIds.Count > 4)
        {
            return Result.Failure<AnalysisResult>(Domain.Errors.DomainErrors.Analysis.TooManyDocuments);
        }

        // Object-level authorization: the caller must own (or be admin over) every document.
        var accessResult = await EnsureDocumentAccessAsync(
            _currentUser, request.DocumentIds, cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result.Failure<AnalysisResult>(accessResult.Error);
        }

        var stopwatch = Stopwatch.StartNew();

        LogStartingAnalysis(_logger, request.Capability, request.DocumentIds.Count);

        // --- Resolve the AnalysisCapability enum from the string value ---
        if (!Enum.TryParse<AnalysisCapability>(request.Capability, ignoreCase: true, out var capability))
        {
            capability = AnalysisCapability.CustomQuestion;
        }

        var ownerId = _currentUser.UserId ?? Guid.Empty;

        // --- Create and persist an AnalysisSession (Pending) ---
        AnalysisSession? session = null;
        Guid? sessionId = null;

        if (ownerId != Guid.Empty)
        {
            var sessionResult = AnalysisSession.Create(
                ownerId,
                request.DocumentIds,
                capability,
                request.CustomQuestion);

            if (sessionResult.IsSuccess)
            {
                session = sessionResult.Value;
                try
                {
                    var sessionRepo = UnitOfWork.Repository<AnalysisSession>();
                    await sessionRepo.AddAsync(session, cancellationToken);
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                    sessionId = session.Id;
                    session.MarkInProgress();
                    sessionRepo.Update(session);
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    LogSessionPersistenceFailed(_logger, "AnalysisSession", ex);
                    session = null;
                    sessionId = null;
                }
            }
        }

        // --- RAG retrieval ---
        var query = string.IsNullOrWhiteSpace(request.CustomQuestion)
            ? request.Capability
            : request.CustomQuestion;

        var context = await RetrieveContextAsync(
            query, request.DocumentIds, ContextChunks, cancellationToken);

        // --- AI completion ---
        var (systemPrompt, userPrompt) = PromptTemplates.BuildAnalysisPrompt(
            request.Capability, request.CustomQuestion, context);

        var completionResult = await CompleteAsync(systemPrompt, userPrompt, cancellationToken);

        if (completionResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, completionResult.Error.Description, cancellationToken);
            return Result.Failure<AnalysisResult>(completionResult.Error);
        }

        var parseResult = ParseJson<JsonAnalysisResultDto>(completionResult.Value.Content);

        if (parseResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, parseResult.Error.Description, cancellationToken);
            return Result.Failure<AnalysisResult>(parseResult.Error);
        }

        var dto = parseResult.Value;

        var analysisResult = new AnalysisResult(
            dto.ExecutiveSummary,
            dto.KeyFindings.Select(MapKeyFinding).ToList(),
            dto.Risks.Select(MapRiskItem).ToList(),
            dto.Recommendations.Select(MapRecommendation).ToList(),
            dto.ActionItems.Select(MapActionItem).ToList(),
            MapCitations(dto.Sources));

        stopwatch.Stop();

        // --- Complete the session and record usage metric in a single commit ---
        // Batching both writes into one SaveChangesAsync makes completion + usage atomic:
        // a persistence failure after AI has responded leaves no orphaned InProgress row.
        if (session is not null)
        {
            try
            {
                var domainUsage = new DomainTokenUsage(
                    completionResult.Value.Usage.PromptTokens,
                    completionResult.Value.Usage.CompletionTokens,
                    completionResult.Value.Usage.EstimatedCost);

                var domainCitations = MapToDomainCitations(dto.Sources);

                var keyFindings = dto.KeyFindings.Select(f => f.Title).ToList();
                var risks = dto.Risks.Select(r => r.Title).ToList();
                var recommendations = dto.Recommendations.Select(r => r.Title).ToList();
                var actionItems = dto.ActionItems.Select(a => a.Description).ToList();

                session.Complete(
                    dto.ExecutiveSummary,
                    keyFindings,
                    risks,
                    recommendations,
                    actionItems,
                    domainCitations,
                    domainUsage,
                    stopwatch.Elapsed);

                UnitOfWork.Repository<AnalysisSession>().Update(session);

                // Enlist usage metric into the same Unit of Work (no commit yet)
                await EnlistUsageMetricAsync(
                    ownerId,
                    OperationType,
                    completionResult.Value.Usage,
                    stopwatch.Elapsed,
                    sessionId: sessionId,
                    cancellationToken);

                // Single commit — session completion and usage metric are atomic
                await UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogSessionPersistenceFailed(_logger, "AnalysisSession.Complete", ex);
            }
        }
        else if (ownerId != Guid.Empty)
        {
            // No session was created (e.g., anonymous call); track usage independently
            try
            {
                await EnlistUsageMetricAsync(
                    ownerId,
                    OperationType,
                    completionResult.Value.Usage,
                    stopwatch.Elapsed,
                    sessionId: null,
                    cancellationToken);
                await UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogSessionPersistenceFailed(_logger, "AiUsageMetric", ex);
            }
        }

        LogAnalysisCompleted(_logger, request.Capability, stopwatch.ElapsedMilliseconds);

        return Result.Success(analysisResult);
    }

    // ---- Private helpers ----

    private async Task TryMarkSessionFailedAsync(
        AnalysisSession? session,
        string reason,
        CancellationToken cancellationToken)
    {
        if (session is null)
        {
            return;
        }

        try
        {
            session.MarkFailed(reason);
            var repo = UnitOfWork.Repository<AnalysisSession>();
            repo.Update(session);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogSessionPersistenceFailed(_logger, "AnalysisSession.Failed", ex);
        }
    }

    /// <summary>
    /// Maps JSON citation DTOs to domain <see cref="DomainCitation"/> value objects,
    /// silently skipping any that fail validation so that partial results still succeed.
    /// </summary>
    private static List<DomainCitation> MapToDomainCitations(
        IEnumerable<JsonCitationDto> dtos)
    {
        var results = new List<DomainCitation>();
        foreach (var dto in dtos)
        {
            if (!Guid.TryParse(dto.DocumentId, out var documentId))
            {
                continue;
            }

            var result = DomainCitation.Create(
                documentId,
                dto.DocumentName,
                dto.PageNumber,
                dto.ParagraphReference,
                dto.Snippet,
                dto.ConfidenceScore);

            if (result.IsSuccess)
            {
                results.Add(result.Value);
            }
        }

        return results;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Session persistence failed for '{SessionType}' — AI result still returned to caller")]
    private static partial void LogSessionPersistenceFailed(
        ILogger logger, string sessionType, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting {Capability} analysis for {DocumentCount} document(s)")]
    private static partial void LogStartingAnalysis(
        ILogger logger, string capability, int documentCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Analysis '{Capability}' completed in {ElapsedMs} ms")]
    private static partial void LogAnalysisCompleted(
        ILogger logger, string capability, long elapsedMs);
}
