using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Chat;

namespace AI.DocumentIntelligence.Application.Features.Chat;

/// <summary>
/// Sends a user message to the RAG chat pipeline grounded in the specified documents.
/// </summary>
/// <param name="SessionId">The chat session identifier.</param>
/// <param name="DocumentIds">The documents the conversation is grounded against (at least 1).</param>
/// <param name="Message">The user's question.</param>
/// <param name="History">Prior conversation turns for context.</param>
public sealed record ChatCommand(
    Guid SessionId,
    IReadOnlyList<Guid> DocumentIds,
    string Message,
    IReadOnlyList<ChatTurn> History) : ICommand<ChatResponse>;
