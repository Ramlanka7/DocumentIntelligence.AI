using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Documents.Delete;

/// <summary>
/// Verifies ownership (or admin role), deletes the file from storage, removes the search
/// index entries, removes the entity, and commits the unit of work.
/// </summary>
internal sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    ISearchService searchService,
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

        var deleteFileResult = await fileStorage.DeleteAsync(document.StoragePath, cancellationToken);
        if (deleteFileResult.IsFailure)
        {
            return deleteFileResult;
        }

        // Remove chunks from the vector/hybrid search index. Required for external indexes
        // (e.g. Azure AI Search) that are not covered by the DB-level cascade delete.
        var deleteIndexResult = await searchService.DeleteByDocumentAsync(request.Id, cancellationToken);
        if (deleteIndexResult.IsFailure)
        {
            return deleteIndexResult;
        }

        documentRepository.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
