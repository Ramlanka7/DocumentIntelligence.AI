namespace AI.DocumentIntelligence.Infrastructure.Documents.Chunking;

/// <summary>
/// Configuration options for the document chunking strategy. Bound from the
/// <c>Chunking</c> section of <c>appsettings.json</c>.
/// </summary>
internal sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    /// <summary>Maximum number of characters per chunk (sliding window size).</summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>Number of characters to overlap between consecutive chunks to preserve context.</summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// When true, the chunker splits first by detected document sections (headings) before
    /// applying the sliding window, ensuring chunks don't span section boundaries.
    /// </summary>
    public bool SectionAware { get; set; } = true;

    /// <summary>Minimum number of characters for a chunk to be indexed (filters out near-empty slices).</summary>
    public int MinChunkSize { get; set; } = 50;
}
