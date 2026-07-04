using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>Repository for <see cref="AuditLog"/> entries with audit-specific queries.</summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>Returns all audit log entries for the specified user.</summary>
    public Task<IReadOnlyList<AuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
