using AI.DocumentIntelligence.Domain.Enums;

namespace AI.DocumentIntelligence.Application.Features.Documents.Upload;

/// <summary>The response returned after a document has been successfully uploaded and queued for ingestion.</summary>
/// <param name="DocumentId">The newly created document's identifier.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="Status">The document's lifecycle status immediately after upload.</param>
public sealed record UploadDocumentResponse(
    Guid DocumentId,
    string FileName,
    DocumentStatus Status);
