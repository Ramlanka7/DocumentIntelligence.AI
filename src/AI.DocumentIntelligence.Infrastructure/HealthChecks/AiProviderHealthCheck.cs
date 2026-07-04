using AI.DocumentIntelligence.Infrastructure.AI.Options;
using AI.DocumentIntelligence.Infrastructure.AI.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the configured AI provider has its required credentials present in configuration.
/// No real API call is made; the check is configuration-only so that health checks remain fast
/// and free of side-effects.  Returns <see cref="HealthStatus.Degraded"/> when required
/// settings are absent or when the selected provider is a stub (not yet fully implemented).
/// </summary>
internal sealed class AiProviderHealthCheck(
    IOptions<AiProviderOptions> providerOptions,
    IConfiguration configuration) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var providerName = string.IsNullOrWhiteSpace(providerOptions.Value.ProviderName)
            ? AzureOpenAiProvider.ProviderName
            : providerOptions.Value.ProviderName;

        var (isConfigured, message) = CheckProvider(providerName);

        var result = isConfigured
            ? HealthCheckResult.Healthy(
                message,
                new Dictionary<string, object> { ["provider"] = providerName })
            : HealthCheckResult.Degraded(
                message,
                data: new Dictionary<string, object> { ["provider"] = providerName });

        return Task.FromResult(result);
    }

    private (bool isConfigured, string message) CheckProvider(string providerName)
    {
        if (providerName == AzureOpenAiProvider.ProviderName)
        {
            var endpoint = configuration[$"{AzureOpenAIOptions.SectionName}:Endpoint"];
            var apiKey = configuration[$"{AzureOpenAIOptions.SectionName}:ApiKey"];

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                return (false,
                    $"AI provider '{providerName}' is not fully configured (missing Endpoint or ApiKey).");
            }

            return (true, $"AI provider '{providerName}' is configured.");
        }

        if (providerName == AnthropicProvider.ProviderName)
        {
            var apiKey = configuration[$"{AnthropicOptions.SectionName}:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false,
                    $"AI provider '{providerName}' is not fully configured (missing ApiKey).");
            }

            return (true, $"AI provider '{providerName}' is configured.");
        }

        // OpenAI and Ollama remain stubs.
        if (providerName is OpenAiProvider.ProviderName or OllamaProvider.ProviderName)
        {
            return (false,
                $"AI provider '{providerName}' is a stub — full implementation not yet available.");
        }

        return (false, $"Unknown AI provider '{providerName}'.");
    }
}
