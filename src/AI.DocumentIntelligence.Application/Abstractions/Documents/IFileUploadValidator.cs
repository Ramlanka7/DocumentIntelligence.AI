using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Documents;

/// <summary>
/// Validates a batch of files before they are stored or processed. Implementations must inspect
/// the actual file content (magic bytes) rather than relying on the client-supplied extension or
/// MIME type, and must enforce per-file size, combined-size, document-count, and page-count limits
/// as configured in <c>UploadOptions</c>.
/// </summary>
public interface IFileUploadValidator
{
    /// <summary>
    /// Validates all files in the batch.
    /// Returns <see cref="Result.Success()"/> when every check passes,
    /// or a <see cref="Result.Failure(Error)"/> describing the first violation.
    /// Never throws for expected failures.
    /// </summary>
    public Result Validate(IReadOnlyList<UploadedFile> files);
}
