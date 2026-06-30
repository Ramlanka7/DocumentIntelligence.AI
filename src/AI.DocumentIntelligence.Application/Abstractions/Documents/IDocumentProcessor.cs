using AI.DocumentIntelligence.Application.Contracts.Documents;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Documents;

/// <summary>
/// Extracts text and structure from an uploaded document. One implementation exists per supported
/// format (PDF, DOCX, TXT, CSV, PPTX); the active processor is selected via <see cref="CanProcess"/>,
/// so new formats drop in without changing callers.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>Whether this processor can handle the given file name/content type.</summary>
    /// <param name="fileName">The original file name (used for extension matching).</param>
    /// <param name="contentType">The MIME content type.</param>
    public bool CanProcess(string fileName, string contentType);

    /// <summary>Extracts text, structure and metadata from the document stream.</summary>
    /// <param name="content">The document content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The extraction result, or a failure <see cref="Result"/> if processing fails.</returns>
    public Task<Result<DocumentExtractionResult>> ProcessAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
