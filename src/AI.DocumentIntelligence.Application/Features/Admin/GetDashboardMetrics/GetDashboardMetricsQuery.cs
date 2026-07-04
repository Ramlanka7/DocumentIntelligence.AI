using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Admin.GetDashboardMetrics;

/// <summary>
/// Returns aggregated platform metrics for the admin dashboard. All filter parameters are optional.
/// </summary>
/// <param name="DateFrom">Inclusive lower bound (UTC) for the metric window.</param>
/// <param name="DateTo">Inclusive upper bound (UTC) for the metric window.</param>
/// <param name="OperationType">Filter AI usage by operation type (Analysis|Comparison|Chat).</param>
/// <param name="UserId">Filter AI usage by a specific user identifier.</param>
public sealed record GetDashboardMetricsQuery(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? OperationType,
    Guid? UserId) : IQuery<DashboardMetricsDto>;
