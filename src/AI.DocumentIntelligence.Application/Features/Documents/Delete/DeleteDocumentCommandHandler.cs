using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Application.Features.Documents.Delete;

/// <summary>
/// Verifies ownership (or admin role), removes the entity (committing the unit of work first),
/// then cleans up the stored file and search index entries best-effort.
///
/// Ordering rationale: the database row is the source of truth. Deleting it first means a
/// mid-operation failure can only leave an orphaned blob or index entry (harmless, logged,
/// re-cleanable) — never a listed document whose underlying file has already been destroyed.
/// </summary>
internal sealed partial class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    ISearchService searchService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<DeleteDocumentCommandHandler> logger)
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

        var storagePath = document.StoragePath;

        // 1. Remove the entity and commit. This also removes the extracted text held on the row.
        documentRepository.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Best-effort cleanup of the search index. Azure AI Search is the sole store for
        //    chunk content and embeddings, so nothing in the database removes them. Failure
        //    leaves stale entries that are filtered out at query time by document ID;
        //    log and continue.
        var deleteIndexResult = await searchService.DeleteByDocumentAsync(request.Id, cancellationToken);
        if (deleteIndexResult.IsFailure)
        {
            LogIndexCleanupFailed(logger, request.Id, deleteIndexResult.Error.Description);
        }

        // 3. Best-effort cleanup of the stored file. Failure leaves an orphaned blob; log so
        //    operations can sweep it up.
        var deleteFileResult = await fileStorage.DeleteAsync(storagePath, cancellationToken);
        if (deleteFileResult.IsFailure)
        {
            LogFileCleanupFailed(logger, request.Id, storagePath, deleteFileResult.Error.Description);
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Search index cleanup failed for deleted document {DocumentId}: {Error}")]
    private static partial void LogIndexCleanupFailed(ILogger logger, Guid documentId, string error);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "File cleanup failed for deleted document {DocumentId} at '{StoragePath}': {Error}")]
    private static partial void LogFileCleanupFailed(
        ILogger logger, Guid documentId, string storagePath, string error);
}
