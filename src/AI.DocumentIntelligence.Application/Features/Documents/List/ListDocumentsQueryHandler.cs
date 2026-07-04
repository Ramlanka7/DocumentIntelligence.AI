using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Features.Documents.List;

/// <summary>Returns all documents owned by the authenticated user.</summary>
internal sealed class ListDocumentsQueryHandler(
    IDocumentRepository documentRepository,
    ICurrentUser currentUser)
    : IQueryHandler<ListDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>
{
    public async Task<Result<IReadOnlyList<DocumentSummaryDto>>> Handle(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyList<DocumentSummaryDto>>(
                Error.Unauthorized("Auth.NotAuthenticated", "The user is not authenticated."));
        }

        var documents = await documentRepository.GetByOwnerAsync(currentUser.UserId.Value, cancellationToken);

        var dtos = documents
            .Select(d => new DocumentSummaryDto(
                d.Id,
                d.Metadata.FileName,
                d.Status,
                d.Type,
                d.Metadata.SizeBytes,
                new DateTimeOffset(d.CreatedAtUtc, TimeSpan.Zero)))
            .ToList();

        return Result.Success<IReadOnlyList<DocumentSummaryDto>>(dtos);
    }
}
