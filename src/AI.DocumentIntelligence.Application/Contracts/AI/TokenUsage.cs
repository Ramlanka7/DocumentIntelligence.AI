namespace AI.DocumentIntelligence.Application.Contracts.AI;

/// <summary>Token consumption reported by an AI provider for a single call.</summary>
/// <param name="PromptTokens">Tokens consumed by the prompt.</param>
/// <param name="CompletionTokens">Tokens produced in the completion.</param>
public sealed record TokenUsage(int PromptTokens, int CompletionTokens)
{
    /// <summary>Total tokens billed for the call.</summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}
