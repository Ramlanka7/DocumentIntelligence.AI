using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSession;

/// <summary>Returns a single chat session, enforcing that only the owner may view it.</summary>
internal sealed class GetChatSessionQueryHandler(
    IChatSessionRepository chatSessionRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetChatSessionQuery, ChatSessionDetailDto>
{
    public async Task<Result<ChatSessionDetailDto>> Handle(
        GetChatSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await chatSessionRepository
            .GetByIdWithMessagesAsync(request.SessionId, cancellationToken);

        if (session is null || session.OwnerId != currentUser.UserId)
        {
            return Result.Failure<ChatSessionDetailDto>(DomainErrors.Chat.SessionNotFound);
        }

        var messages = session.Messages
            .OrderBy(m => m.Ordinal)
            .Select(m => new ChatMessageDto(
                Id: m.Id,
                Ordinal: m.Ordinal,
                Role: m.Role.ToString(),
                Content: m.Content,
                Citations: m.Citations
                    .Select(c => new ChatMessageCitationDto(
                        DocumentName: c.DocumentName,
                        Page: c.PageNumber,
                        Paragraph: c.ParagraphReference,
                        Confidence: c.ConfidenceScore))
                    .ToList()
                    .AsReadOnly(),
                CreatedAt: new DateTimeOffset(m.CreatedAtUtc, TimeSpan.Zero)))
            .ToList()
            .AsReadOnly();

        var updatedAt = session.UpdatedAtUtc.HasValue
            ? (DateTimeOffset?)new DateTimeOffset(session.UpdatedAtUtc.Value, TimeSpan.Zero)
            : null;

        var dto = new ChatSessionDetailDto(
            Id: session.Id,
            DocumentIds: session.DocumentIds.ToList().AsReadOnly(),
            Status: session.Status.ToString(),
            Messages: messages,
            CreatedAt: new DateTimeOffset(session.CreatedAtUtc, TimeSpan.Zero),
            UpdatedAt: updatedAt);

        return Result.Success(dto);
    }
}
