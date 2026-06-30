namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Processing state shared by analysis, comparison, and chat sessions.</summary>
public enum SessionStatus
{
    /// <summary>Created but not yet started.</summary>
    Pending = 0,

    /// <summary>The AI operation is running.</summary>
    InProgress = 1,

    /// <summary>Completed successfully with results available.</summary>
    Completed = 2,

    /// <summary>The operation failed.</summary>
    Failed = 3,
}
