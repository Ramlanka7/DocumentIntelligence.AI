namespace AI.DocumentIntelligence.Infrastructure.AI.Options;

/// <summary>
/// Configuration options for the Anthropic Claude provider. Bound from the
/// <c>Anthropic</c> section of <c>appsettings.json</c> / environment variables.
/// Set <c>AI:ProviderName = "Anthropic"</c> to activate this provider.
/// </summary>
internal sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    /// <summary>Anthropic API key (starts with "sk-ant-...").</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Claude model to use for chat completions (e.g. "claude-sonnet-4-6").</summary>
    public string Model { get; set; } = "claude-sonnet-4-6";

    /// <summary>
    /// Maximum number of tokens to generate per response.
    /// Anthropic requires this field; 8192 is a safe default for most tasks.
    /// </summary>
    public int MaxTokens { get; set; } = 8192;

    /// <summary>Cost per 1 million input tokens in USD. Update when Anthropic pricing changes.</summary>
    public decimal InputCostPerMillionTokens { get; set; } = 3.0m;

    /// <summary>Cost per 1 million output tokens in USD. Update when Anthropic pricing changes.</summary>
    public decimal OutputCostPerMillionTokens { get; set; } = 15.0m;
}
