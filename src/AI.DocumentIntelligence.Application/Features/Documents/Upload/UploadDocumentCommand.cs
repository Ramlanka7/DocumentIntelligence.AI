using AI.DocumentIntelligence.Application.Common.Messaging;

namespace AI.DocumentIntelligence.Application.Features.Documents.Upload;

/// <summary>
/// Uploads, processes, and ingests a single document into the RAG pipeline.
/// The controller unpacks the <c>IFormFile</c> into primitive values before dispatching.
/// </summary>
/// <param name="FileName">The original file name (e.g. <c>report.pdf</c>).</param>
/// <param name="ContentType">The MIME content type declared by the client.</param>
/// <param name="SizeBytes">Total byte length of the upload.</param>
/// <param name="Content">A readable, seekable stream for the file content.</param>
public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<UploadDocumentResponse>;
