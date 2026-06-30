namespace AI.DocumentIntelligence.Application.Contracts.Comparison;

/// <summary>The nature of a single difference found when comparing documents.</summary>
public enum DifferenceType
{
    /// <summary>Content present in the later/target document but not the earlier/source one.</summary>
    Added = 0,

    /// <summary>Content present in the earlier/source document but absent from the later/target one.</summary>
    Removed = 1,

    /// <summary>Content that exists in both documents but changed.</summary>
    Modified = 2,
}
