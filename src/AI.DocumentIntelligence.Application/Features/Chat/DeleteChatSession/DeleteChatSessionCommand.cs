using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Chat.DeleteChatSession;

/// <summary>Deletes a chat session owned by the current user. Returns 404 if not found or not owned.</summary>
/// <param name="SessionId">The identifier of the session to delete.</param>
public sealed record DeleteChatSessionCommand(Guid SessionId) : ICommand;
