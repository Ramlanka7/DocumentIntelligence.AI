namespace AI.DocumentIntelligence.Infrastructure.AI.Options;

/// <summary>
/// Configuration options for Azure AI Search. Bound from the
/// <c>AzureSearch</c> section of <c>appsettings.json</c>.
/// </summary>
internal sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    /// <summary>The Azure AI Search service endpoint URI (e.g. https://&lt;name&gt;.search.windows.net).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>The Azure AI Search admin API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The name of the search index used for document chunks.</summary>
    public string IndexName { get; set; } = "document-chunks";

    /// <summary>
    /// The name of the semantic configuration within the index to use for semantic re-ranking.
    /// Set to empty string to disable semantic search.
    /// </summary>
    public string SemanticConfigurationName { get; set; } = "document-chunks-semantic";

    /// <summary>The vector field dimensionality. Must match <see cref="AzureOpenAIOptions.EmbeddingDimensions"/>.</summary>
    public int VectorDimensions { get; set; } = 1536;

    /// <summary>
    /// Maximum number of chunk IDs retrieved per page when deleting all chunks for a document.
    /// Azure AI Search limits a single fetch to 1000; increase pages rather than this value.
    /// </summary>
    public int MaxDeleteBatchSize { get; set; } = 1000;
}
