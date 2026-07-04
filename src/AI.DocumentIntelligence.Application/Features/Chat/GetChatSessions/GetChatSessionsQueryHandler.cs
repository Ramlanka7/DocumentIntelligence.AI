using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;

/// <summary>Returns the current user's chat sessions, owner-scoped for security.</summary>
internal sealed class GetChatSessionsQueryHandler(
    IChatSessionRepository chatSessionRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetChatSessionsQuery, IReadOnlyList<ChatSessionSummaryDto>>
{
    public async Task<Result<IReadOnlyList<ChatSessionSummaryDto>>> Handle(
        GetChatSessionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<ChatSessionSummaryDto>>(DomainErrors.Auth.Unauthenticated);
        }

        var ownerId = currentUser.UserId.Value;

        var sessions = await chatSessionRepository.GetByOwnerAsync(ownerId, cancellationToken);

        var dtos = sessions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s =>
            {
                // Use the text of the first user message as the title
                var firstUserMessage = s.Messages
                    .OrderBy(m => m.Ordinal)
                    .FirstOrDefault(m => m.Role == ChatRole.User);

                var title = firstUserMessage?.Content ?? string.Empty;
                var updatedAt = s.UpdatedAtUtc.HasValue
                    ? (DateTimeOffset?)new DateTimeOffset(s.UpdatedAtUtc.Value, TimeSpan.Zero)
                    : null;

                return new ChatSessionSummaryDto(
                    Id: s.Id,
                    Title: title,
                    DocumentIds: s.DocumentIds.ToList().AsReadOnly(),
                    Status: s.Status.ToString(),
                    MessageCount: s.Messages.Count,
                    CreatedAt: new DateTimeOffset(s.CreatedAtUtc, TimeSpan.Zero),
                    UpdatedAt: updatedAt);
            })
            .ToList();

        return Result.Success<IReadOnlyList<ChatSessionSummaryDto>>(dtos);
    }
}
