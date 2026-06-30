namespace AI.DocumentIntelligence.Application.Contracts.Chat;

/// <summary>A user's question within a RAG chat session scoped to a set of documents.</summary>
/// <param name="SessionId">The chat session the question belongs to.</param>
/// <param name="DocumentIds">The documents the chat is grounded against.</param>
/// <param name="Message">The user's question.</param>
/// <param name="History">Prior conversation turns for context.</param>
public sealed record ChatRequest(
    Guid SessionId,
    IReadOnlyList<Guid> DocumentIds,
    string Message,
    IReadOnlyList<ChatTurn> History);
