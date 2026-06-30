namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>Classification of a single change in a comparison change log (GitHub-style diff).</summary>
public enum ChangeStatus
{
    /// <summary>Content present in the later document but not the earlier one.</summary>
    Added = 0,

    /// <summary>Content present in the earlier document but not the later one.</summary>
    Removed = 1,

    /// <summary>Content present in both but altered.</summary>
    Modified = 2,
}
