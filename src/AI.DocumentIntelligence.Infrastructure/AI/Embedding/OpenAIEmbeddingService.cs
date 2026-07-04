using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Infrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace AI.DocumentIntelligence.Infrastructure.AI.Embedding;

/// <summary>
/// Generates embedding vectors via the direct OpenAI embeddings API (non-Azure).
/// Used when <c>AzureOpenAI:Endpoint</c> is not configured — for example, when
/// Anthropic is the selected chat provider and OpenAI is used only for embeddings.
/// Configured by <see cref="OpenAIOptions"/>.
/// </summary>
internal sealed partial class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var openAiClient = new OpenAIClient(_options.ApiKey);
        _client = openAiClient.GetEmbeddingClient(_options.EmbeddingModel);
    }

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

    public async Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<IReadOnlyList<float>>>([]);
        }

        try
        {
            LogGeneratingEmbeddings(_logger, inputs.Count, _options.EmbeddingModel);

            var response = await _client.GenerateEmbeddingsAsync(
                inputs,
                cancellationToken: cancellationToken);

            var embeddings = response.Value
                .Select(e => (IReadOnlyList<float>)e.ToFloats().ToArray())
                .ToList();

            LogReceivedEmbeddings(_logger, embeddings.Count);

            return Result.Success<IReadOnlyList<IReadOnlyList<float>>>(embeddings);
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex);
            return Result.Failure<IReadOnlyList<IReadOnlyList<float>>>(
                Error.Failure("Embedding.UnexpectedError",
                    $"Unexpected error generating OpenAI embeddings: {ex.Message}"));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Generating embeddings for {InputCount} input(s) via OpenAI model '{Model}'")]
    private static partial void LogGeneratingEmbeddings(ILogger logger, int inputCount, string model);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Received {EmbeddingCount} OpenAI embedding(s)")]
    private static partial void LogReceivedEmbeddings(ILogger logger, int embeddingCount);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unexpected error generating OpenAI embeddings")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
