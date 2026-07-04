using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Contracts.AI;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Infrastructure.AI.Providers;

/// <summary>
/// Stub <see cref="IAIProvider"/> for OpenAI (non-Azure). Selectable via
/// <c>AI:ProviderName = "OpenAI"</c> in configuration. A full implementation should replace this
/// stub when direct OpenAI access is required.
/// </summary>
internal sealed class OpenAiProvider : IAIProvider
{
    /// <summary>Stable identifier used for provider selection and telemetry.</summary>
    public const string ProviderName = "OpenAI";

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public Task<Result<AiCompletionResult>> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            Result.Failure<AiCompletionResult>(
                Error.Failure(
                    "OpenAI.NotConfigured",
                    "The OpenAI provider is not yet configured. Set AI:ProviderName to 'AzureOpenAI' or provide a full OpenAI implementation.")));
}
