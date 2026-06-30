namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Base type for all persistent aggregates and entities: a surrogate <see cref="Id"/>, audit
/// timestamps, and a buffer of <see cref="IDomainEvent"/>s raised during a unit of work.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>EF / serialization constructor; assigns a new surrogate key.</summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>Initializes the entity with an explicit surrogate key.</summary>
    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    /// <summary>The surrogate primary key.</summary>
    public Guid Id { get; protected set; }

    /// <summary>When the entity was created (UTC). Set by the persistence layer.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the entity was last modified (UTC), or <see langword="null"/> if never. Set by the persistence layer.</summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Domain events raised by this entity and not yet dispatched.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Buffers a domain event to be dispatched after the unit of work commits.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears the buffered domain events once they have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
