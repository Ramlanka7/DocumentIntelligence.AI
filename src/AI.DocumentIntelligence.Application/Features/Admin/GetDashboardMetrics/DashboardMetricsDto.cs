namespace AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;

// ── Per-day AI usage ──────────────────────────────────────────────────────────

/// <summary>Token usage and cost for a single calendar day.</summary>
/// <param name="Date">The date in YYYY-MM-DD format.</param>
/// <param name="PromptTokens">Total prompt tokens consumed on this date.</param>
/// <param name="CompletionTokens">Total completion tokens produced on this date.</param>
/// <param name="Cost">Total estimated cost (USD) on this date.</param>
/// <param name="OperationCount">Number of AI operations on this date.</param>
public sealed record DailyUsagePointDto(
    string Date,
    long PromptTokens,
    long CompletionTokens,
    decimal Cost,
    int OperationCount);

// ── Per operation-type usage ─────────────────────────────────────────────────

/// <summary>Aggregated AI usage broken down by operation type.</summary>
/// <param name="Type">The operation type (e.g. "analysis", "comparison", "chat").</param>
/// <param name="Count">Number of operations of this type.</param>
/// <param name="PromptTokens">Total prompt tokens for this type.</param>
/// <param name="CompletionTokens">Total completion tokens for this type.</param>
/// <param name="Cost">Total estimated cost (USD) for this type.</param>
public sealed record UsageByTypeDto(
    string Type,
    int Count,
    long PromptTokens,
    long CompletionTokens,
    decimal Cost);

// ── AI usage summary ─────────────────────────────────────────────────────────

/// <summary>Aggregated AI usage summary for the admin dashboard.</summary>
/// <param name="TotalPromptTokens">Total prompt tokens across all AI operations in the window.</param>
/// <param name="TotalCompletionTokens">Total completion tokens across all AI operations in the window.</param>
/// <param name="TotalCost">Total estimated cost (USD) across all AI operations in the window.</param>
/// <param name="AverageProcessingTimeMs">Average processing time in milliseconds across all AI operations.</param>
/// <param name="DailyUsage">Per-day usage breakdown.</param>
/// <param name="UsageByType">Per-operation-type usage breakdown.</param>
public sealed record AiUsageSummaryDto(
    long TotalPromptTokens,
    long TotalCompletionTokens,
    decimal TotalCost,
    double AverageProcessingTimeMs,
    IReadOnlyList<DailyUsagePointDto> DailyUsage,
    IReadOnlyList<UsageByTypeDto> UsageByType);

// ── Recent activity ───────────────────────────────────────────────────────────

/// <summary>A single recent activity event shown in the admin dashboard feed.</summary>
/// <param name="Id">The event identifier.</param>
/// <param name="Type">The event type (e.g. "analysis", "comparison", "chat", "upload", "login").</param>
/// <param name="UserId">The acting user's identifier.</param>
/// <param name="UserEmail">The acting user's email address.</param>
/// <param name="Description">Human-readable description of the event.</param>
/// <param name="Timestamp">ISO 8601 timestamp of the event (UTC).</param>
public sealed record ActivityItemDto(
    string Id,
    string Type,
    string UserId,
    string UserEmail,
    string Description,
    string Timestamp);

// ── Dashboard aggregate ───────────────────────────────────────────────────────

/// <summary>
/// Aggregated platform metrics for the admin dashboard. Field names are camelCase-serialised to
/// match the frontend <c>DashboardMetrics</c> TypeScript interface exactly.
/// </summary>
/// <param name="TotalUsers">Total number of registered platform users.</param>
/// <param name="TotalDocuments">Total number of uploaded documents.</param>
/// <param name="TotalAnalyses">Total number of analysis sessions.</param>
/// <param name="TotalComparisons">Total number of comparison sessions.</param>
/// <param name="TotalChatSessions">Total number of chat sessions.</param>
/// <param name="AiUsage">Aggregated AI token-usage and cost metrics.</param>
/// <param name="RecentActivity">Latest ~15 platform activity events.</param>
public sealed record DashboardMetricsDto(
    int TotalUsers,
    int TotalDocuments,
    int TotalAnalyses,
    int TotalComparisons,
    int TotalChatSessions,
    AiUsageSummaryDto AiUsage,
    IReadOnlyList<ActivityItemDto> RecentActivity);
