using AI.DocumentIntelligence.Application.Abstractions.Documents;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Application.Common.Messaging;
using AI.DocumentIntelligence.Application.Features.RAG.Ingest;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.Errors;
using AI.DocumentIntelligence.Domain.ValueObjects;
using MediatR;

namespace AI.DocumentIntelligence.Application.Features.Documents.Upload;

/// <summary>
/// Validates → processes (extract text) → stores → persists → ingests a single uploaded document.
/// Steps: validate file, detect type, extract text, save to storage, create domain entity, dispatch ingest.
/// </summary>
internal sealed class UploadDocumentCommandHandler(
    IFileUploadValidator fileUploadValidator,
    IDocumentProcessorFactory processorFactory,
    IFileStorage fileStorage,
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISender sender)
    : ICommandHandler<UploadDocumentCommand, UploadDocumentResponse>
{
    public async Task<Result<UploadDocumentResponse>> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate the upload (size, type, etc.)
        var uploadedFile = new UploadedFile(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            0,
            request.Content);

        var validationResult = fileUploadValidator.Validate([uploadedFile]);
        if (validationResult.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(validationResult.Error);
        }

        // 2. Resolve processor and extract text
        var processorResult = processorFactory.Resolve(request.FileName, request.ContentType);
        if (processorResult.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(processorResult.Error);
        }

        if (request.Content.CanSeek)
        {
            request.Content.Seek(0, SeekOrigin.Begin);
        }

        var extractionResult = await processorResult.Value.ProcessAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            cancellationToken);

        if (extractionResult.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(extractionResult.Error);
        }

        // 3. Save file to storage
        if (request.Content.CanSeek)
        {
            request.Content.Seek(0, SeekOrigin.Begin);
        }

        var storageResult = await fileStorage.SaveAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            cancellationToken);

        if (storageResult.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(storageResult.Error);
        }

        // 4. Detect document type from extension
        var documentType = DetectDocumentType(request.FileName);

        // 5. Create and persist the document entity
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<UploadDocumentResponse>(
                Error.Unauthorized("Auth.NotAuthenticated", "The user is not authenticated."));
        }

        var ownerId = currentUser.UserId.Value;
        var metadata = new FileMetadata(
            request.FileName,
            request.SizeBytes,
            extractionResult.Value.Metadata.PageCount,
            request.ContentType);

        var document = Document.Create(ownerId, metadata, documentType, storageResult.Value);
        document.MarkProcessing();
        document.MarkProcessed(extractionResult.Value.FullText);

        await documentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Dispatch the RAG ingest pipeline (fire-and-forget within the same request context)
        await sender.Send(
            new IngestDocumentCommand(
                document.Id,
                request.FileName,
                extractionResult.Value),
            cancellationToken);

        return Result.Success(
            new UploadDocumentResponse(document.Id, request.FileName, document.Status));
    }

    private static DocumentType DetectDocumentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => DocumentType.Pdf,
            ".docx" => DocumentType.Docx,
            ".txt" => DocumentType.Txt,
            ".csv" => DocumentType.Csv,
            ".pptx" => DocumentType.Pptx,
            _ => DocumentType.Txt,
        };
    }
}
