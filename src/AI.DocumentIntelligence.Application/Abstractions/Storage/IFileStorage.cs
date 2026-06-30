using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.Storage;

/// <summary>
/// Abstraction over binary file storage for uploaded documents (e.g. Azure Blob Storage or the local
/// file system), returning an opaque storage key that callers persist and use for later retrieval.
/// </summary>
public interface IFileStorage
{
    /// <summary>Stores a file and returns an opaque key identifying it.</summary>
    /// <param name="content">The file content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The storage key, or a failure <see cref="Result"/>.</returns>
    public Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a previously stored file for reading.</summary>
    /// <param name="storageKey">The key returned by <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A readable stream, or a failure <see cref="Result"/>.</returns>
    public Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously stored file.</summary>
    /// <param name="storageKey">The key returned by <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A success or failure <see cref="Result"/>.</returns>
    public Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
