using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Documents.Queries;

/// <summary>
/// Returns full document detail for the owner (or an admin), or
/// <see cref="DomainErrors.Document.NotFound"/> when absent / not accessible.
/// Non-owners receive NotFound rather than Forbidden so document IDs are not enumerable.
/// </summary>
internal sealed class GetDocumentQueryHandler(
    IDocumentRepository documentRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetDocumentQuery, DocumentDetailDto>
{
    public async Task<Result<DocumentDetailDto>> Handle(
        GetDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<DocumentDetailDto>(
                Error.Unauthorized("Auth.NotAuthenticated", "The user is not authenticated."));
        }

        var document = await documentRepository.GetByIdAsync(request.Id, cancellationToken);

        var isAdmin = currentUser.Roles.Contains("Admin");
        if (document is null || (!isAdmin && document.OwnerId != currentUser.UserId))
        {
            return Result.Failure<DocumentDetailDto>(DomainErrors.Document.NotFound);
        }

        var dto = new DocumentDetailDto(
            document.Id,
            document.Metadata.FileName,
            document.Status,
            document.Type,
            document.Metadata.SizeBytes,
            document.Metadata.PageCount,
            new DateTimeOffset(document.CreatedAtUtc, TimeSpan.Zero),
            document.FailureReason);

        return Result.Success(dto);
    }
}
