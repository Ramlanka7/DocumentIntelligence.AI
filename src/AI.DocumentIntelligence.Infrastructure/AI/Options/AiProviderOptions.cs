namespace AI.DocumentIntelligence.Infrastructure.AI.Options;

/// <summary>
/// Selects the active AI completion provider. Bound from the <c>AI</c> section of
/// <c>appsettings.json</c>. Switching the value requires no code change in callers.
/// </summary>
internal sealed class AiProviderOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// The name of the active provider. Accepted values: <c>"AzureOpenAI"</c> (default),
    /// <c>"OpenAI"</c>, <c>"Anthropic"</c>, <c>"Ollama"</c>.
    /// </summary>
    public string ProviderName { get; set; } = "AzureOpenAI";
}
