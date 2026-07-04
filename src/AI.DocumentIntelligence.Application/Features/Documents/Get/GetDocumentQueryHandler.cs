using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;

namespace AI.DocumentIntelligence.Application.Features.Documents.Queries;

/// <summary>Returns full document detail, or <see cref="DomainErrors.Document.NotFound"/> when absent.</summary>
internal sealed class GetDocumentQueryHandler(IDocumentRepository documentRepository)
    : IQueryHandler<GetDocumentQuery, DocumentDetailDto>
{
    public async Task<Result<DocumentDetailDto>> Handle(
        GetDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (document is null)
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
