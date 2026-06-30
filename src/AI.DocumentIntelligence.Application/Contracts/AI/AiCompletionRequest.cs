namespace AI.DocumentIntelligence.Application.Contracts.AI;

/// <summary>A provider-agnostic request for a chat completion.</summary>
/// <param name="Messages">The ordered conversation messages forming the prompt.</param>
/// <param name="Model">Optional model/deployment override; falls back to the provider default when null.</param>
/// <param name="Temperature">Sampling temperature (0.0–2.0).</param>
/// <param name="MaxTokens">Optional cap on completion tokens.</param>
public sealed record AiCompletionRequest(
    IReadOnlyList<AiMessage> Messages,
    string? Model = null,
    double Temperature = 0.2,
    int? MaxTokens = null);
