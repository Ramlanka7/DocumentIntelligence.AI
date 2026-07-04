using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Contracts.Chat;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Chat;

/// <summary>Delegates to <see cref="IChatService"/> and forwards the result.</summary>
internal sealed class ChatCommandHandler(IChatService chatService)
    : ICommandHandler<ChatCommand, ChatResponse>
{
    public async Task<Result<ChatResponse>> Handle(
        ChatCommand request,
        CancellationToken cancellationToken)
    {
        var chatRequest = new ChatRequest(
            request.SessionId,
            request.DocumentIds,
            request.Message,
            request.History);

        return await chatService.AskAsync(chatRequest, cancellationToken);
    }
}
