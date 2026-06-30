namespace AI.DocumentIntelligence.Domain.ValueObjects;

/// <summary>
/// Token consumption and cost figures for a single AI call, used both on session results and
/// in the <see cref="Entities.AiUsageMetric"/> ledger.
/// </summary>
/// <param name="PromptTokens">Tokens consumed by the prompt.</param>
/// <param name="CompletionTokens">Tokens produced in the completion.</param>
/// <param name="EstimatedCost">Estimated monetary cost of the call, in USD.</param>
public sealed record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCost)
{
    /// <summary>The total tokens billed (prompt + completion).</summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>A zero-usage value for initializing sessions before any AI call is made.</summary>
    public static readonly TokenUsage None = new(0, 0, 0m);
}
