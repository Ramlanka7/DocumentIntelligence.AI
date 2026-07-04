using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Errors;
using Microsoft.Extensions.Logging;

namespace AI.DocumentIntelligence.Infrastructure.Storage;

/// <summary>
/// Local file-system implementation of <see cref="IFileStorage"/>.
/// Stores files under <c>%TEMP%/DocumentIntelligence/</c> using a
/// <c>{guid}_{fileName}</c> key format. Intended as a development stub;
/// replace with Azure Blob Storage for production.
/// </summary>
public sealed partial class LocalFileStorage(ILogger<LocalFileStorage> logger) : IFileStorage
{
    private static readonly string StorageRoot =
        Path.Combine(Path.GetTempPath(), "DocumentIntelligence");

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(StorageRoot);

            var key = $"{Guid.NewGuid():N}_{fileName}";
            var filePath = Path.Combine(StorageRoot, key);

            // Rewind in case the caller left the stream partially read.
            if (content.CanSeek)
            {
                content.Seek(0, SeekOrigin.Begin);
            }

            await using var fs = File.Create(filePath);
            await content.CopyToAsync(fs, cancellationToken);

            LogFileSaved(logger, key, filePath);
            return Result.Success(key);
        }
        catch (Exception ex)
        {
            LogSaveFailed(logger, fileName, ex);
            return Result.Failure<string>(
                Error.Failure("Storage.SaveFailed", $"Failed to save file '{fileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(StorageRoot, storageKey);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(
                    Result.Failure<Stream>(DomainErrors.Document.NotFound));
            }

            Stream stream = File.OpenRead(filePath);
            return Task.FromResult(Result.Success(stream));
        }
        catch (Exception ex)
        {
            LogGetFailed(logger, storageKey, ex);
            return Task.FromResult(
                Result.Failure<Stream>(
                    Error.Failure("Storage.GetFailed", $"Failed to retrieve file '{storageKey}': {ex.Message}")));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(StorageRoot, storageKey);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                LogFileDeleted(logger, storageKey);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogDeleteFailed(logger, storageKey, ex);
            return Task.FromResult(
                Result.Failure(
                    Error.Failure("Storage.DeleteFailed", $"Failed to delete file '{storageKey}': {ex.Message}")));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stored file {Key} at {Path}")]
    private static partial void LogFileSaved(ILogger logger, string key, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save file {FileName}")]
    private static partial void LogSaveFailed(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to retrieve file {Key}")]
    private static partial void LogGetFailed(ILogger logger, string key, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted file {Key}")]
    private static partial void LogFileDeleted(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete file {Key}")]
    private static partial void LogDeleteFailed(ILogger logger, string key, Exception exception);
}
