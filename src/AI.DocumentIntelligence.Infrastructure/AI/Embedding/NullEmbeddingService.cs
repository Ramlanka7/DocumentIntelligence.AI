using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Infrastructure.AI.Embedding;

/// <summary>
/// Stand-in IEmbeddingService registered when no embedding credentials are configured.
/// Returns a typed failure on every call so callers surface a clear "not configured" error
/// instead of a UriFormatException thrown during DI resolution.
/// </summary>
internal sealed class NullEmbeddingService : IEmbeddingService
{
    private static readonly Error NotConfiguredError = Error.Failure(
        "Embedding.NotConfigured",
        "No embedding provider is configured. Set AzureOpenAI:Endpoint + AzureOpenAI:ApiKey, " +
        "or OpenAI:ApiKey, to enable embedding generation.");

    public Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<IReadOnlyList<float>>(NotConfiguredError));

    public Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<IReadOnlyList<IReadOnlyList<float>>>(NotConfiguredError));
}
