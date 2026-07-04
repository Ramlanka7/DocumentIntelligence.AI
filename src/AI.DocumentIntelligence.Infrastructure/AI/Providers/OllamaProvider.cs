using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Infrastructure.AI.Providers;

/// <summary>
/// Stub <see cref="IAIProvider"/> for Ollama (self-hosted open-source models). Selectable via
/// <c>AI:ProviderName = "Ollama"</c> in configuration. A full implementation backed by the Ollama
/// HTTP API should replace this stub when local-model access is required.
/// </summary>
internal sealed class OllamaProvider : IAIProvider
{
    /// <summary>Stable identifier used for provider selection and telemetry.</summary>
    public const string ProviderName = "Ollama";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Result.Failure<AiCompletionResult>(
                Error.Failure(
                    "Ollama.NotConfigured",
                    "The Ollama provider is not yet configured. Set AI:ProviderName to 'AzureOpenAI' or provide a full Ollama implementation.")));
}
