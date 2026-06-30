namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Marker for something significant that has happened in the domain. Raised by entities and
/// dispatched after a successful unit-of-work commit (handled in later tasks).
/// </summary>
public interface IDomainEvent
{
    /// <summary>The instant the event occurred.</summary>
    public DateTime OccurredOnUtc { get; }
}
