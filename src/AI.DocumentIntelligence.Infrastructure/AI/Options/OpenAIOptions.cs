namespace AI.DocumentIntelligence.Infrastructure.AI.Options;

/// <summary>
/// Configuration options for the direct OpenAI API (non-Azure). Bound from the
/// <c>OpenAI</c> section of <c>appsettings.json</c> / environment variables.
/// Used automatically as the embedding back-end when <c>AzureOpenAI:Endpoint</c>
/// is not configured — for example, when Anthropic is selected as the chat provider.
/// </summary>
internal sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>OpenAI API key (starts with "sk-...").</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Embedding model name (e.g. "text-embedding-3-small").</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>Expected embedding vector dimensionality — must match the model.</summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}
