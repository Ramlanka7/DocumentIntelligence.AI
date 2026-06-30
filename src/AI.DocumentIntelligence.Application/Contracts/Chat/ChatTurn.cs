using AI.DocumentIntelligence.Application.Contracts.AI;

namespace AI.DocumentIntelligence.Application.Contracts.Chat;

/// <summary>A single prior turn in a document chat conversation.</summary>
/// <param name="Role">Who authored the turn.</param>
/// <param name="Content">The message text.</param>
public sealed record ChatTurn(AiRole Role, string Content);
