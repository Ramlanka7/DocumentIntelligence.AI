using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Application.Abstractions.AI;

/// <summary>
/// Abstraction over a large-language-model chat provider. Implementations exist for Azure OpenAI
/// (the default), OpenAI, Anthropic Claude and Ollama; the active provider is configurable, so new
/// providers can be added behind this interface without changing the higher-level AI services.
/// </summary>
public interface IAIProvider
{
    /// <summary>A stable provider identifier (e.g. "AzureOpenAI") used for selection and telemetry.</summary>
    public string Name { get; }

    /// <summary>Generates a chat completion for the supplied prompt.</summary>
    /// <param name="request">The provider-agnostic completion request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The completion and its token usage, or a failure <see cref="Result"/>.</returns>
    public Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default);
}
