using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.Identity;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Infrastructure.Auth;

/// <summary>
/// Creates an <see cref="AuditLog"/> entry and enqueues it on the Unit of Work.
/// The caller is responsible for committing via <c>SaveChangesAsync</c> so that
/// the audit record and any business entities are saved in a single transaction.
/// </summary>
internal sealed class AuditService(
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IAuditService
{
    public Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Create(
            userId: currentUser.UserId,
            action: action,
            entityType: entityType,
            entityId: entityId,
            details: details,
            ipAddress: currentUser.IpAddress);

        return unitOfWork.Repository<AuditLog>().AddAsync(log, ct);
    }
}
