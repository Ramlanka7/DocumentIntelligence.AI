namespace AI.DocumentIntelligence.Application.Contracts.AI;

/// <summary>A provider-agnostic chat completion together with its usage and the model that produced it.</summary>
/// <param name="Content">The generated text.</param>
/// <param name="Usage">Token usage for the call.</param>
/// <param name="Model">The model/deployment that produced the completion.</param>
public sealed record AiCompletionResult(string Content, TokenUsage Usage, string Model);
