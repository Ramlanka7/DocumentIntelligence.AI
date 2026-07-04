using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;

/// <summary>Returns a summary list of the current user's chat sessions.</summary>
public sealed record GetChatSessionsQuery : IQuery<IReadOnlyList<ChatSessionSummaryDto>>;
