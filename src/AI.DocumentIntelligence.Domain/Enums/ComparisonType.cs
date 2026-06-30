namespace AI.DocumentIntelligence.Domain.Enums;

/// <summary>The kind of comparison performed across documents in a <see cref="Entities.ComparisonSession"/>.</summary>
public enum ComparisonType
{
    /// <summary>General side-by-side comparison.</summary>
    SideBySide = 0,

    /// <summary>Comparison of successive versions of the same document.</summary>
    Version = 1,

    /// <summary>Contract-focused comparison (clauses, terms, obligations).</summary>
    Contract = 2,

    /// <summary>Policy-focused comparison.</summary>
    Policy = 3,

    /// <summary>A free-form, user-defined comparison.</summary>
    Custom = 4,
}
