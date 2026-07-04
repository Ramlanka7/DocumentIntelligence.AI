namespace AI.DocumentIntelligence.Infrastructure.Auth;

/// <summary>Strongly-typed binding for the <c>Upload</c> configuration section.</summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Maximum size (bytes) for a single file. Default 50 MB.</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;

    /// <summary>Maximum combined size (bytes) of all files in a batch. Default 200 MB.</summary>
    public long MaxCombinedSizeBytes { get; set; } = 209_715_200;

    /// <summary>Maximum number of documents per upload batch.</summary>
    public int MaxDocuments { get; set; } = 4;

    /// <summary>Maximum combined page count across all files in a batch.</summary>
    public int MaxCombinedPages { get; set; } = 500;
}
