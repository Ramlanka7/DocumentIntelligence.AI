using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;

/// <summary>
/// Aggregates platform-wide metrics from Users, Documents, Sessions, and AiUsageMetrics.
/// Data access goes exclusively through <see cref="IUnitOfWork"/> — no DbContext in Application.
/// </summary>
internal sealed class GetDashboardMetricsQueryHandler(
    IUnitOfWork unitOfWork,
    IUserRepository userRepository)
    : IQueryHandler<GetDashboardMetricsQuery, DashboardMetricsDto>
{
    private const int MaxRecentActivity = 15;

    public async Task<Result<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery request,
        CancellationToken cancellationToken)
    {
        // ── Counts ────────────────────────────────────────────────────────────
        var users = await userRepository.GetAllAsync(cancellationToken);
        var documents = await unitOfWork.Repository<Document>().GetAllAsync(cancellationToken);
        var analysisSessions = await unitOfWork.Repository<AnalysisSession>().GetAllAsync(cancellationToken);
        var comparisonSessions = await unitOfWork.Repository<ComparisonSession>().GetAllAsync(cancellationToken);
        var chatSessions = await unitOfWork.Repository<ChatSession>().GetAllAsync(cancellationToken);
        var auditLogs = await unitOfWork.Repository<AuditLog>().GetAllAsync(cancellationToken);

        // ── AI usage metrics — push all supplied filters to the database ──────
        // Build a predicate so that the WHERE clause is evaluated by PostgreSQL rather
        // than pulling the full table into memory and filtering in .NET.
        var fromUtc = request.DateFrom?.UtcDateTime;
        var toUtc = request.DateTo?.UtcDateTime;
        var opType = request.OperationType;
        var userId = request.UserId;

        var hasFilter = fromUtc.HasValue || toUtc.HasValue ||
                        !string.IsNullOrWhiteSpace(opType) || userId.HasValue;

        IReadOnlyList<AiUsageMetric> metricsList;
        if (hasFilter)
        {
            metricsList = await unitOfWork.Repository<AiUsageMetric>().FindAsync(
                m =>
                    (!fromUtc.HasValue || m.CreatedAtUtc >= fromUtc.Value) &&
                    (!toUtc.HasValue || m.CreatedAtUtc <= toUtc.Value) &&
                    (string.IsNullOrEmpty(opType) || m.OperationType == opType) &&
                    (!userId.HasValue || m.UserId == userId.Value),
                cancellationToken);
        }
        else
        {
            metricsList = await unitOfWork.Repository<AiUsageMetric>().GetAllAsync(cancellationToken);
        }

        // ── Aggregate AI usage ────────────────────────────────────────────────
        var totalPromptTokens = metricsList.Sum(m => (long)m.TokenUsage.PromptTokens);
        var totalCompletionTokens = metricsList.Sum(m => (long)m.TokenUsage.CompletionTokens);
        var totalCost = metricsList.Sum(m => m.TokenUsage.EstimatedCost);
        var averageProcessingTimeMs = metricsList.Count > 0
            ? metricsList.Average(m => m.ProcessingTime.TotalMilliseconds)
            : 0.0;

        // Daily usage (grouped by date, UTC)
        var dailyUsage = metricsList
            .GroupBy(m => m.CreatedAtUtc.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyUsagePointDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(m => (long)m.TokenUsage.PromptTokens),
                g.Sum(m => (long)m.TokenUsage.CompletionTokens),
                g.Sum(m => m.TokenUsage.EstimatedCost),
                g.Count()))
            .ToList();

        // Usage by operation type — normalise to lowercase to match frontend enum ('analysis' | 'comparison' | 'chat')
        var usageByType = metricsList
            .GroupBy(m => m.OperationType, StringComparer.OrdinalIgnoreCase)
            .Select(g => new UsageByTypeDto(
                g.Key.ToLowerInvariant(),
                g.Count(),
                g.Sum(m => (long)m.TokenUsage.PromptTokens),
                g.Sum(m => (long)m.TokenUsage.CompletionTokens),
                g.Sum(m => m.TokenUsage.EstimatedCost)))
            .OrderBy(u => u.Type)
            .ToList();

        var aiUsage = new AiUsageSummaryDto(
            totalPromptTokens,
            totalCompletionTokens,
            totalCost,
            averageProcessingTimeMs,
            dailyUsage,
            usageByType);

        // ── Recent activity feed ──────────────────────────────────────────────
        // Build a lookup from userId → email for activity annotations
        var userEmailMap = users.ToDictionary(u => u.Id, u => u.Email);

        var recentActivity = BuildRecentActivity(
            analysisSessions,
            comparisonSessions,
            chatSessions,
            documents,
            auditLogs,
            userEmailMap);

        // ── Return ────────────────────────────────────────────────────────────
        var dto = new DashboardMetricsDto(
            TotalUsers: users.Count,
            TotalDocuments: documents.Count,
            TotalAnalyses: analysisSessions.Count,
            TotalComparisons: comparisonSessions.Count,
            TotalChatSessions: chatSessions.Count,
            AiUsage: aiUsage,
            RecentActivity: recentActivity);

        return Result.Success(dto);
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<ActivityItemDto> BuildRecentActivity(
        IReadOnlyList<AnalysisSession> analysisSessions,
        IReadOnlyList<ComparisonSession> comparisonSessions,
        IReadOnlyList<ChatSession> chatSessions,
        IReadOnlyList<Document> documents,
        IReadOnlyList<AuditLog> auditLogs,
        Dictionary<Guid, string> userEmailMap)
    {
        var items = new List<(DateTime Timestamp, ActivityItemDto Item)>();

        // Analysis sessions
        foreach (var s in analysisSessions)
        {
            var email = userEmailMap.TryGetValue(s.OwnerId, out var e) ? e : "unknown";
            items.Add((s.CreatedAtUtc, new ActivityItemDto(
                Id: s.Id.ToString(),
                Type: "analysis",
                UserId: s.OwnerId.ToString(),
                UserEmail: email,
                Description: $"Analysis ({s.Capability}) — {s.Status}",
                Timestamp: s.CreatedAtUtc.ToString("o"))));
        }

        // Comparison sessions
        foreach (var s in comparisonSessions)
        {
            var email = userEmailMap.TryGetValue(s.OwnerId, out var e) ? e : "unknown";
            items.Add((s.CreatedAtUtc, new ActivityItemDto(
                Id: s.Id.ToString(),
                Type: "comparison",
                UserId: s.OwnerId.ToString(),
                UserEmail: email,
                Description: $"Comparison ({s.ComparisonType}) — {s.Status}",
                Timestamp: s.CreatedAtUtc.ToString("o"))));
        }

        // Chat sessions
        foreach (var s in chatSessions)
        {
            var email = userEmailMap.TryGetValue(s.OwnerId, out var e) ? e : "unknown";
            items.Add((s.CreatedAtUtc, new ActivityItemDto(
                Id: s.Id.ToString(),
                Type: "chat",
                UserId: s.OwnerId.ToString(),
                UserEmail: email,
                Description: $"Chat session — {s.Status}",
                Timestamp: s.CreatedAtUtc.ToString("o"))));
        }

        // Document uploads — derive from audit logs where Action contains "Uploaded"
        foreach (var log in auditLogs.Where(l => l.Action.Contains("Uploaded", StringComparison.OrdinalIgnoreCase)))
        {
            var userId = log.UserId?.ToString() ?? string.Empty;
            var email = log.UserId.HasValue && userEmailMap.TryGetValue(log.UserId.Value, out var e) ? e : "unknown";
            items.Add((log.CreatedAtUtc, new ActivityItemDto(
                Id: log.Id.ToString(),
                Type: "upload",
                UserId: userId,
                UserEmail: email,
                Description: log.Details ?? "Document uploaded",
                Timestamp: log.CreatedAtUtc.ToString("o"))));
        }

        // Login events from audit logs
        foreach (var log in auditLogs.Where(l => l.Action.Contains("LoggedIn", StringComparison.OrdinalIgnoreCase)))
        {
            var userId = log.UserId?.ToString() ?? string.Empty;
            var email = log.UserId.HasValue && userEmailMap.TryGetValue(log.UserId.Value, out var e) ? e : "unknown";
            items.Add((log.CreatedAtUtc, new ActivityItemDto(
                Id: log.Id.ToString(),
                Type: "login",
                UserId: userId,
                UserEmail: email,
                Description: "User logged in",
                Timestamp: log.CreatedAtUtc.ToString("o"))));
        }

        return items
            .OrderByDescending(x => x.Timestamp)
            .Take(MaxRecentActivity)
            .Select(x => x.Item)
            .ToList()
            .AsReadOnly();
    }
}
