namespace AI.DocumentIntelligence.Application.Contracts.AI;

/// <summary>A single message in a prompt sent to an <c>IAIProvider</c>.</summary>
/// <param name="Role">The role of the message author.</param>
/// <param name="Content">The message text.</param>
public sealed record AiMessage(AiRole Role, string Content);
