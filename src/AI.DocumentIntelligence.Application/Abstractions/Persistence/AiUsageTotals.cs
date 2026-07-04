namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>Summary totals returned by <see cref="IAiUsageMetricRepository.GetTotalsAsync"/>.</summary>
public sealed record AiUsageTotals(
    long TotalPromptTokens,
    long TotalCompletionTokens,
    decimal TotalEstimatedCost,
    double AverageProcessingTimeMs);
