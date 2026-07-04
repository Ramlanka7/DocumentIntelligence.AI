using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Chat.DeleteChatSession;

/// <summary>Deletes the chat session, enforcing owner-scoping via <see cref="ICurrentUser"/>.</summary>
internal sealed class DeleteChatSessionCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteChatSessionCommand>
{
    public async Task<Result> Handle(
        DeleteChatSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await unitOfWork.Repository<ChatSession>()
            .GetByIdAsync(request.SessionId, cancellationToken);

        if (session is null || session.OwnerId != currentUser.UserId)
        {
            return Result.Failure(DomainErrors.Chat.SessionNotFound);
        }

        unitOfWork.Repository<ChatSession>().Remove(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
