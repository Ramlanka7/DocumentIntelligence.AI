using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace AI.DocumentIntelligence.Infrastructure.AI.Embedding;

/// <summary>
/// Generates embedding vectors via the Azure OpenAI embeddings endpoint.
/// Configured by <see cref="AzureOpenAIOptions"/> (options pattern — no hardcoded secrets).
/// </summary>
internal sealed partial class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient? _client;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AzureOpenAIEmbeddingService> _logger;

    public AzureOpenAIEmbeddingService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.Endpoint) && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var azureClient = new AzureOpenAIClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));
            _client = azureClient.GetEmbeddingClient(_options.EmbeddingDeployment);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var batchResult = await GenerateEmbeddingsAsync([input], cancellationToken);

        if (batchResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<float>>(batchResult.Error);
        }

        return Result.Success(batchResult.Value[0]);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            return Result.Failure<IReadOnlyList<IReadOnlyList<float>>>(
                Error.Failure("Embedding.NotConfigured",
                    "Azure OpenAI embedding is not configured. Set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey."));
        }

        if (inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<IReadOnlyList<float>>>([]);
        }

        try
        {
            LogGeneratingEmbeddings(_logger, inputs.Count, _options.EmbeddingDeployment);

            var response = await _client.GenerateEmbeddingsAsync(
                inputs,
                cancellationToken: cancellationToken);

            var embeddings = response.Value
                .Select(e => (IReadOnlyList<float>)e.ToFloats().ToArray())
                .ToList();

            LogReceivedEmbeddings(_logger, embeddings.Count);

            return Result.Success<IReadOnlyList<IReadOnlyList<float>>>(embeddings);
        }
        catch (RequestFailedException ex)
        {
            LogRequestFailed(_logger, ex.Status, ex.ErrorCode ?? "unknown", ex);
            return Result.Failure<IReadOnlyList<IReadOnlyList<float>>>(
                Error.Failure("Embedding.RequestFailed", $"Azure OpenAI embeddings request failed ({ex.Status}): {ex.Message}"));
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex);
            return Result.Failure<IReadOnlyList<IReadOnlyList<float>>>(
                Error.Failure("Embedding.UnexpectedError", $"Unexpected error generating embeddings: {ex.Message}"));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Generating embeddings for {InputCount} input(s) via deployment '{Deployment}'")]
    private static partial void LogGeneratingEmbeddings(ILogger logger, int inputCount, string deployment);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received {EmbeddingCount} embedding(s)")]
    private static partial void LogReceivedEmbeddings(ILogger logger, int embeddingCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Azure OpenAI embeddings request failed: {Status} {ErrorCode}")]
    private static partial void LogRequestFailed(ILogger logger, int status, string errorCode, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error generating embeddings")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
