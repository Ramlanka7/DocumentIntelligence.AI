namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Lifecycle stage of an uploaded <see cref="Entities.Document"/> as it is ingested into the RAG pipeline.</summary>
public enum DocumentStatus
{
    /// <summary>Uploaded but not yet processed.</summary>
    Pending = 0,

    /// <summary>The file transfer to storage is in progress.</summary>
    Uploading = 1,

    /// <summary>Text extraction, chunking, and embedding are in progress.</summary>
    Processing = 2,

    /// <summary>Fully processed and available for analysis, comparison, and chat.</summary>
    Processed = 3,

    /// <summary>Processing failed; see the document's error detail.</summary>
    Failed = 4,
}
