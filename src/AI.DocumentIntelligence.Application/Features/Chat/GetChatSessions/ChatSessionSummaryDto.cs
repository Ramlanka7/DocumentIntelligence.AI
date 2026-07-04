namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;

/// <summary>Summary view of a chat session for list endpoints.</summary>
/// <param name="Id">The session's unique identifier.</param>
/// <param name="Title">The text of the first user message, used as the session title. Empty when no messages exist.</param>
/// <param name="DocumentIds">The document identifiers the session is grounded in.</param>
/// <param name="Status">The session status (Pending | InProgress | Completed | Failed).</param>
/// <param name="MessageCount">Total number of messages in the session.</param>
/// <param name="CreatedAt">When the session was created (UTC).</param>
/// <param name="UpdatedAt">When the session was last modified (UTC), or null if never.</param>
public sealed record ChatSessionSummaryDto(
    Guid Id,
    string Title,
    IReadOnlyList<Guid> DocumentIds,
    string Status,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
