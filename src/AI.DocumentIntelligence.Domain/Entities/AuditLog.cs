using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Domain.Entities;

/// <summary>
/// An append-only record of a security- or business-relevant action, satisfying the platform's
/// audit logging requirement. Uses <see cref="BaseEntity.CreatedAtUtc"/> as the event timestamp.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    private AuditLog()
    {
        // EF / serialization constructor.
        Action = string.Empty;
        EntityType = string.Empty;
    }

    private AuditLog(
        Guid id,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? details,
        string? ipAddress)
        : base(id)
    {
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
        IpAddress = ipAddress;
    }

    /// <summary>The acting user, or <see langword="null"/> for system-initiated events.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>A stable, machine-readable action code, e.g. <c>"Document.Uploaded"</c>.</summary>
    public string Action { get; private set; }

    /// <summary>The type of entity affected, e.g. <c>"Document"</c>.</summary>
    public string EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? Details { get; private set; }

    public string? IpAddress { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        string? ipAddress = null) =>
        new(Guid.NewGuid(), userId, action, entityType, entityId, details, ipAddress);
}
