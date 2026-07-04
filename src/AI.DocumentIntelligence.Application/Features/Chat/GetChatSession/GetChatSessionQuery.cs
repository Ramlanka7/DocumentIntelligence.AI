using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;

/// <summary>Returns one chat session with its ordered messages. Returns 404 if not found or not owned by the current user.</summary>
/// <param name="SessionId">The identifier of the chat session to retrieve.</param>
public sealed record GetChatSessionQuery(Guid SessionId) : IQuery<ChatSessionDetailDto>;
