using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Documents.Delete;

/// <summary>
/// Verifies ownership (or admin role), deletes the file from storage, removes the entity,
/// and commits the unit of work.
/// </summary>
internal sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Result> Handle(
        DeleteDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure(DomainErrors.Document.NotFound);
        }

        // Authorise: owner or admin may delete.
        var isAdmin = currentUser.Roles.Contains("Admin");
        if (!isAdmin && document.OwnerId != currentUser.UserId)
        {
            return Result.Failure(
                Error.Forbidden("Document.Forbidden", "You do not have permission to delete this document."));
        }

        var deleteResult = await fileStorage.DeleteAsync(document.StoragePath, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        documentRepository.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
