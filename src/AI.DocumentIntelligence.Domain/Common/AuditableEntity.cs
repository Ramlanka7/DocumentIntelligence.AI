namespace AI.DocumentIntelligence.Domain.Common;

/// <summary>
/// Base class for entities that carry creation/modification audit timestamps. The audit fields and
/// surrogate key are inherited from <see cref="BaseEntity"/>; this type exists as the explicit base
/// for auditable aggregates referenced across the domain.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>EF / serialization constructor; assigns a new surrogate key.</summary>
    protected AuditableEntity()
        : base()
    {
    }

    /// <summary>Initializes the entity with an explicit surrogate key.</summary>
    protected AuditableEntity(Guid id)
        : base(id)
    {
    }
}
