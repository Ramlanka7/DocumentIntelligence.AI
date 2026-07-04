using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorage"/>.
/// Stores uploaded documents in a configurable blob container, returning an opaque
/// <c>{guid:N}/{sanitizedFileName}</c> key that callers persist and use for later retrieval.
///
/// Activated automatically when <c>AzureStorage:ConnectionString</c> is non-empty in
/// configuration. Falls back to <see cref="LocalFileStorage"/> when the connection string
/// is absent (development default). See <see cref="BlobStorageOptions"/> for all settings.
/// </summary>
internal sealed partial class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    public AzureBlobFileStorage(
        BlobServiceClient serviceClient,
        IOptions<BlobStorageOptions> options,
        ILogger<AzureBlobFileStorage> logger)
    {
        _serviceClient = serviceClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _serviceClient.GetBlobContainerClient(_options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);

            // Sanitize: strip path separators that could escape the intended "folder".
            var sanitized = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "upload";
            }

            var key = $"{Guid.NewGuid():N}/{sanitized}";
            var blobClient = containerClient.GetBlobClient(key);

            // Rewind in case the caller left the stream partially read.
            if (content.CanSeek)
            {
                content.Seek(0, SeekOrigin.Begin);
            }

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            };

            await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

            LogBlobSaved(_logger, key, _options.ContainerName);
            return Result.Success(key);
        }
        catch (RequestFailedException ex)
        {
            LogSaveRequestFailed(_logger, fileName, ex.Status, ex);
            return Result.Failure<string>(
                Error.Failure("Storage.SaveFailed",
                    $"Azure Blob upload failed for '{fileName}' ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogSaveFailed(_logger, fileName, ex);
            return Result.Failure<string>(
                Error.Failure("Storage.SaveFailed",
                    $"Failed to upload file '{fileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _serviceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(storageKey);

            // Download to a seekable MemoryStream so callers can read multiple times.
            var ms = new MemoryStream();
            var response = await blobClient.DownloadToAsync(ms, cancellationToken);

            if (response.IsError)
            {
                LogGetFailed(_logger, storageKey, response.Status);
                return Result.Failure<Stream>(
                    Error.Failure("Storage.GetFailed",
                        $"Azure Blob download failed for '{storageKey}': HTTP {response.Status}"));
            }

            ms.Seek(0, SeekOrigin.Begin);
            LogBlobRetrieved(_logger, storageKey, _options.ContainerName);
            return Result.Success<Stream>(ms);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            LogBlobNotFound(_logger, storageKey);
            return Result.Failure<Stream>(DomainErrors.Document.NotFound);
        }
        catch (RequestFailedException ex)
        {
            LogGetRequestFailed(_logger, storageKey, ex.Status, ex);
            return Result.Failure<Stream>(
                Error.Failure("Storage.GetFailed",
                    $"Azure Blob download failed for '{storageKey}' ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogGetUnexpectedFailed(_logger, storageKey, ex);
            return Result.Failure<Stream>(
                Error.Failure("Storage.GetFailed",
                    $"Failed to retrieve file '{storageKey}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _serviceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(storageKey);

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            LogBlobDeleted(_logger, storageKey, _options.ContainerName);
            return Result.Success();
        }
        catch (RequestFailedException ex)
        {
            LogDeleteRequestFailed(_logger, storageKey, ex.Status, ex);
            return Result.Failure(
                Error.Failure("Storage.DeleteFailed",
                    $"Azure Blob delete failed for '{storageKey}' ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogDeleteFailed(_logger, storageKey, ex);
            return Result.Failure(
                Error.Failure("Storage.DeleteFailed",
                    $"Failed to delete file '{storageKey}': {ex.Message}"));
        }
    }

    // ---- LoggerMessage source-gen logging ----

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Uploaded blob '{Key}' to container '{Container}'")]
    private static partial void LogBlobSaved(ILogger logger, string key, string container);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Azure Blob upload request failed for '{FileName}': HTTP {Status}")]
    private static partial void LogSaveRequestFailed(
        ILogger logger, string fileName, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to upload file '{FileName}'")]
    private static partial void LogSaveFailed(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Retrieved blob '{Key}' from container '{Container}'")]
    private static partial void LogBlobRetrieved(ILogger logger, string key, string container);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Blob '{Key}' not found (404)")]
    private static partial void LogBlobNotFound(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Azure Blob download failed for '{Key}': HTTP {Status}")]
    private static partial void LogGetFailed(ILogger logger, string key, int status);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Azure Blob download request failed for '{Key}': HTTP {Status}")]
    private static partial void LogGetRequestFailed(
        ILogger logger, string key, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to retrieve file '{Key}'")]
    private static partial void LogGetUnexpectedFailed(ILogger logger, string key, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Deleted blob '{Key}' from container '{Container}'")]
    private static partial void LogBlobDeleted(ILogger logger, string key, string container);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Azure Blob delete request failed for '{Key}': HTTP {Status}")]
    private static partial void LogDeleteRequestFailed(
        ILogger logger, string key, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to delete file '{Key}'")]
    private static partial void LogDeleteFailed(ILogger logger, string key, Exception exception);
}
