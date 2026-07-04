using FluentValidation;

namespace AI.DocumentIntelligence.Application.Features.Chat.GetChatSessions;

/// <summary>No inputs to validate on <see cref="GetChatSessionsQuery"/> — the owner is resolved from the ambient user.</summary>
internal sealed class GetChatSessionsQueryValidator : AbstractValidator<GetChatSessionsQuery>
{
    public GetChatSessionsQueryValidator()
    {
        // No query parameters — ownership is enforced in the handler via ICurrentUser.
    }
}
