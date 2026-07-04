using System.Diagnostics;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Infrastructure.AI.Prompts;
using Microsoft.Extensions.Logging;
using DomainCitation = AI.DocumentIntelligence.Domain.ValueObjects.Citation;
using DomainTokenUsage = AI.DocumentIntelligence.Domain.ValueObjects.TokenUsage;

namespace AI.DocumentIntelligence.Infrastructure.AI.Services;

/// <summary>
/// Implements <see cref="IChatService"/> using RAG retrieval and the configured
/// <see cref="IAIProvider"/>. Every answer carries source citations.
/// Each call persists user and assistant <see cref="ChatMessage"/>s in a <see cref="ChatSession"/>.
/// If the request carries an existing session Id the messages are appended; otherwise a new session
/// is created.
/// </summary>
internal sealed partial class ChatService : AiServiceBase, IChatService
{
    private const string OperationType = "Chat";
    private const int ContextChunks = 8;

    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IAIProvider provider,
        ISearchService searchService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ChatService> logger)
        : base(provider, searchService, unitOfWork, logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ChatResponse>> AskAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Result.Failure<ChatResponse>(Domain.Errors.DomainErrors.Chat.EmptyMessage);
        }

        if (request.DocumentIds.Count == 0)
        {
            return Result.Failure<ChatResponse>(Domain.Errors.DomainErrors.Chat.NoDocuments);
        }

        var stopwatch = Stopwatch.StartNew();

        LogStartingChat(_logger, request.SessionId);

        var ownerId = _currentUser.UserId ?? Guid.Empty;

        // --- Load existing session or create a new one ---
        ChatSession? session = null;

        if (ownerId != Guid.Empty)
        {
            session = await LoadOrCreateSessionAsync(request, ownerId, cancellationToken);
        }

        // --- Append user message ---
        ChatMessage? userMessage = null;
        if (session is not null)
        {
            try
            {
                userMessage = session.AddMessage(ChatRole.User, request.Message);
                var sessionRepo = UnitOfWork.Repository<ChatSession>();
                sessionRepo.Update(session);
                await UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogSessionPersistenceFailed(_logger, "ChatSession.UserMessage", ex);
                userMessage = null;
            }
        }

        // --- RAG retrieval ---
        var context = await RetrieveContextAsync(
            request.Message, request.DocumentIds, ContextChunks, cancellationToken);

        // --- AI completion ---
        var (systemPrompt, userPrompt) = PromptTemplates.BuildChatPrompt(request.Message, context);

        var completionResult = await CompleteAsync(systemPrompt, userPrompt, cancellationToken);

        if (completionResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, cancellationToken);
            return Result.Failure<ChatResponse>(completionResult.Error);
        }

        var parseResult = ParseJson<JsonChatResponseDto>(completionResult.Value.Content);

        if (parseResult.IsFailure)
        {
            await TryMarkSessionFailedAsync(session, cancellationToken);
            return Result.Failure<ChatResponse>(parseResult.Error);
        }

        var dto = parseResult.Value;
        var chatResponse = new ChatResponse(
            dto.Answer,
            MapCitations(dto.Citations),
            completionResult.Value.Usage,
            session?.Id ?? request.SessionId);

        stopwatch.Stop();

        // --- Append assistant message + usage metric in a single commit ---
        // Batching both writes makes the assistant turn and usage record atomic.
        if (session is not null)
        {
            try
            {
                var assistantMessage = session.AddMessage(ChatRole.Assistant, dto.Answer);

                var domainUsage = new DomainTokenUsage(
                    completionResult.Value.Usage.PromptTokens,
                    completionResult.Value.Usage.CompletionTokens,
                    completionResult.Value.Usage.EstimatedCost);

                assistantMessage.SetTokenUsage(domainUsage);

                // Attach citations to the assistant message
                foreach (var citationDto in dto.Citations)
                {
                    if (!Guid.TryParse(citationDto.DocumentId, out var documentId))
                    {
                        continue;
                    }

                    var citationResult = DomainCitation.Create(
                        documentId,
                        citationDto.DocumentName,
                        citationDto.PageNumber,
                        citationDto.ParagraphReference,
                        citationDto.Snippet,
                        citationDto.ConfidenceScore);

                    if (citationResult.IsSuccess)
                    {
                        assistantMessage.AddCitation(citationResult.Value);
                    }
                }

                UnitOfWork.Repository<ChatSession>().Update(session);

                // Enlist usage metric into the same Unit of Work (no commit yet)
                await EnlistUsageMetricAsync(
                    ownerId,
                    OperationType,
                    completionResult.Value.Usage,
                    stopwatch.Elapsed,
                    session.Id,
                    cancellationToken);

                // Single commit — assistant message and usage metric are atomic
                await UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogSessionPersistenceFailed(_logger, "ChatSession.AssistantMessage", ex);
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

        LogChatCompleted(_logger, request.SessionId, stopwatch.ElapsedMilliseconds);

        return Result.Success(chatResponse);
    }

    // ---- Private helpers ----

    /// <summary>
    /// Attempts to load an existing <see cref="ChatSession"/> by the request's SessionId.
    /// If the session is not found (or no prior session exists), a new one is created and persisted.
    /// On any persistence failure the method returns <see langword="null"/> so that the AI call
    /// still proceeds without a session record.
    /// </summary>
    private async Task<ChatSession?> LoadOrCreateSessionAsync(
        ChatRequest request,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var sessionRepo = UnitOfWork.Repository<ChatSession>();

        // Try to load an existing session when a non-empty SessionId is provided
        if (request.SessionId != Guid.Empty)
        {
            try
            {
                var existing = await sessionRepo.GetByIdAsync(request.SessionId, cancellationToken);
                if (existing is not null)
                {
                    return existing;
                }
            }
            catch (Exception ex)
            {
                LogSessionPersistenceFailed(_logger, "ChatSession.Load", ex);
                return null;
            }
        }

        // Create a new session
        var sessionResult = ChatSession.Create(ownerId, request.DocumentIds);
        if (sessionResult.IsFailure)
        {
            return null;
        }

        try
        {
            var newSession = sessionResult.Value;
            await sessionRepo.AddAsync(newSession, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            return newSession;
        }
        catch (Exception ex)
        {
            LogSessionPersistenceFailed(_logger, "ChatSession.Create", ex);
            return null;
        }
    }

    private async Task TryMarkSessionFailedAsync(ChatSession? session, CancellationToken cancellationToken)
    {
        if (session is null)
        {
            return;
        }

        try
        {
            session.MarkFailed();
            var repo = UnitOfWork.Repository<ChatSession>();
            repo.Update(session);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogSessionPersistenceFailed(_logger, "ChatSession.Failed", ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Session persistence failed for '{SessionType}' — AI result still returned to caller")]
    private static partial void LogSessionPersistenceFailed(
        ILogger logger, string sessionType, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting chat answer for session {SessionId}")]
    private static partial void LogStartingChat(ILogger logger, Guid sessionId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Chat answer for session {SessionId} completed in {ElapsedMs} ms")]
    private static partial void LogChatCompleted(ILogger logger, Guid sessionId, long elapsedMs);
}
