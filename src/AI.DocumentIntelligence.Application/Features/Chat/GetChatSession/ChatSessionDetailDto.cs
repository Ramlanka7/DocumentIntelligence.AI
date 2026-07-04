namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;

/// <summary>A citation carried on a chat message.</summary>
/// <param name="DocumentName">Human-readable name of the cited document.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="Paragraph">Paragraph/section locator.</param>
/// <param name="Confidence">Confidence score from 0.0 to 1.0.</param>
public sealed record ChatMessageCitationDto(
    string DocumentName,
    int Page,
    string Paragraph,
    double Confidence);

/// <summary>A single message within a chat session.</summary>
/// <param name="Id">The message's unique identifier.</param>
/// <param name="Ordinal">Zero-based position within the session.</param>
/// <param name="Role">The speaker role: "User" or "Assistant".</param>
/// <param name="Content">The text content of the message.</param>
/// <param name="Citations">Source citations grounding an Assistant reply.</param>
/// <param name="CreatedAt">When the message was recorded (UTC).</param>
public sealed record ChatMessageDto(
    Guid Id,
    int Ordinal,
    string Role,
    string Content,
    IReadOnlyList<ChatMessageCitationDto> Citations,
    DateTimeOffset CreatedAt);

/// <summary>Detailed view of a single chat session including all ordered messages.</summary>
/// <param name="Id">The session's unique identifier.</param>
/// <param name="DocumentIds">The document identifiers the session is grounded in.</param>
/// <param name="Status">The session status.</param>
/// <param name="Messages">All messages in ordinal order.</param>
/// <param name="CreatedAt">When the session was created (UTC).</param>
/// <param name="UpdatedAt">When the session was last modified (UTC), or null if never.</param>
public sealed record ChatSessionDetailDto(
    Guid Id,
    IReadOnlyList<Guid> DocumentIds,
    string Status,
    IReadOnlyList<ChatMessageDto> Messages,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
