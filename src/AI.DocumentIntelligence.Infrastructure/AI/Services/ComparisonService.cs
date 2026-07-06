using System.Diagnostics;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Infrastructure.AI.Prompts;
using Microsoft.Extensions.Logging;
using DomainCitation = AI.DocumentIntelligence.Domain.ValueObjects.Citation;
using DomainTokenUsage = AI.DocumentIntelligence.Domain.ValueObjects.TokenUsage;

namespace AI.DocumentIntelligence.Infrastructure.AI.Services;

/// <summary>
/// Implements <see cref="IComparisonService"/> using RAG retrieval and the configured
/// <see cref="IAIProvider"/>. Identifies added, removed, and modified content across 2–4 documents.
/// Results always carry citations. Each call persists a <see cref="ComparisonSession"/> with full
/// citation, change log, and token usage data.
/// </summary>
internal sealed partial class ComparisonService : AiServiceBase, IComparisonService
{
    private const string OperationType = "Comparison";
    private const int ContextChunks = 15;

    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ComparisonService> _logger;

    public ComparisonService(
        IAIProvider provider,
        ISearchService searchService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ComparisonService> logger)
        : base(provider, searchService, unitOfWork, logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ComparisonResult>> CompareAsync(
        ComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.DocumentIds.Count < 2 || request.DocumentIds.Count > 4)
        {
            return Result.Failure<ComparisonResult>(
                Domain.Errors.DomainErrors.Comparison.InsufficientDocuments);
        }

        // Object-level authorization: the caller must own (or be admin over) every document.
        var accessResult = await EnsureDocumentAccessAsync(
            _currentUser, request.DocumentIds, cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result.Failure<ComparisonResult>(accessResult.Error);
        }

        var stopwatch = Stopwatch.StartNew();

        LogStartingComparison(_logger, request.ComparisonType, request.DocumentIds.Count);

        // --- Resolve the ComparisonType enum from the string value ---
        if (!Enum.TryParse<ComparisonType>(request.ComparisonType, ignoreCase: true, out var comparisonType))
        {
            comparisonType = ComparisonType.Custom;
        }

        var ownerId = _currentUser.UserId ?? Guid.Empty;

        // --- Create and persist a ComparisonSession (Pending) ---
        ComparisonSession? session = null;
        Guid? sessionId = null;

        if (ownerId != Guid.Empty)
        {
            var sessionResult = ComparisonSession.Create(ownerId, request.DocumentIds, comparisonType);

            if (sessionResult.IsSuccess)
            {
                session = sessionResult.Value;
                try
                {
                    var sessionRepo = UnitOfWork.Repository<ComparisonSession>();
                    await sessionRepo.AddAsync(session, cancellationToken);
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                    sessionId = session.Id;
                    session.MarkInProgress();
                    sessionRepo.Update(session);
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    LogSessionPersistenceFailed(_logger, "ComparisonSession", ex);
                    session = null;
                    sessionId = null;
                }
            }
        }

        // --- RAG retrieval ---
        var query = string.IsNullOrWhiteSpace(request.CustomInstructions)
            ? $"{request.ComparisonType} comparison"
            : request.CustomInstructions;

        var context = await RetrieveContextAsync(
            query, request.DocumentIds, ContextChunks, cancellationToken);

        // --- AI completion ---
        var (systemPrompt, userPrompt) = PromptTemplates.BuildComparisonPrompt(
            request.ComparisonType, request.CustomInstructions, context);

        var completionResult = await CompleteAsync(systemPrompt, userPrompt, cancellationToken);

        if (completionResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, completionResult.Error.Description, cancellationToken);
            return Result.Failure<ComparisonResult>(completionResult.Error);
        }

        var parseResult = ParseJson<JsonComparisonResultDto>(completionResult.Value.Content);

        if (parseResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, parseResult.Error.Description, cancellationToken);
            return Result.Failure<ComparisonResult>(parseResult.Error);
        }

        var dto = parseResult.Value;

        var comparisonResult = new ComparisonResult(
            dto.ExecutiveOverview,
            dto.Differences.Select(MapDifference).ToList(),
            dto.Risks.Select(MapRiskItem).ToList(),
            dto.Recommendations.Select(MapRecommendation).ToList(),
            MapCitations(dto.Sources));

        stopwatch.Stop();

        // --- Complete the session and record usage metric in a single commit ---
        if (session is not null)
        {
            try
            {
                var domainUsage = new DomainTokenUsage(
                    completionResult.Value.Usage.PromptTokens,
                    completionResult.Value.Usage.CompletionTokens,
                    completionResult.Value.Usage.EstimatedCost);

                var domainCitations = MapToDomainCitations(dto.Sources);
                var keyDifferences = dto.Differences.Select(d => d.Summary).ToList();
                var recommendations = dto.Recommendations.Select(r => r.Title).ToList();

                // Build domain ChangeLogEntry list from differences
                var changeLog = dto.Differences
                    .Select(d => new ChangeLogEntry(
                        d.Type.ToUpperInvariant() switch
                        {
                            "ADDED" => ChangeStatus.Added,
                            "REMOVED" => ChangeStatus.Removed,
                            _ => ChangeStatus.Modified
                        },
                        d.Section,
                        d.Before,
                        d.After,
                        d.Summary))
                    .ToList();

                var riskSummary = dto.Risks.Count > 0
                    ? string.Join("; ", dto.Risks.Select(r => r.Title))
                    : string.Empty;

                session.Complete(
                    dto.ExecutiveOverview,
                    keyDifferences,
                    riskSummary,
                    recommendations,
                    changeLog,
                    domainCitations,
                    domainUsage,
                    stopwatch.Elapsed);

                UnitOfWork.Repository<ComparisonSession>().Update(session);

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
                LogSessionPersistenceFailed(_logger, "ComparisonSession.Complete", ex);
            }
        }
        else if (ownerId != Guid.Empty)
        {
            // No session was created; track usage independently
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

        LogComparisonCompleted(_logger, request.ComparisonType, stopwatch.ElapsedMilliseconds);

        return Result.Success(comparisonResult);
    }

    // ---- Private helpers ----

    private async Task TryMarkSessionFailedAsync(
        ComparisonSession? session,
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
            var repo = UnitOfWork.Repository<ComparisonSession>();
            repo.Update(session);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogSessionPersistenceFailed(_logger, "ComparisonSession.Failed", ex);
        }
    }

    /// <summary>
    /// Maps JSON citation DTOs to domain <see cref="DomainCitation"/> value objects,
    /// silently skipping any that fail validation.
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
        Message = "Starting {ComparisonType} comparison for {DocumentCount} document(s)")]
    private static partial void LogStartingComparison(
        ILogger logger, string comparisonType, int documentCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Comparison '{ComparisonType}' completed in {ElapsedMs} ms")]
    private static partial void LogComparisonCompleted(
        ILogger logger, string comparisonType, long elapsedMs);
}
