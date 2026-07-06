using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Application.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AI.DocumentIntelligence.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IChatSessionRepository"/>. All queries eager-load the
/// <c>Messages</c> navigation and order messages by <c>Ordinal</c> so that handlers always
/// receive fully-populated sessions without additional round-trips.
/// </summary>
internal sealed class ChatSessionRepository(AppDbContext context) : IChatSessionRepository
{
    private readonly AppDbContext _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatSession>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        await _context.Set<ChatSession>()
            .Where(s => s.OwnerId == ownerId)
            .Include(s => s.Messages.OrderBy(m => m.Ordinal))
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(QueryLimits.MaxListResults)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ChatSession?> GetByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await _context.Set<ChatSession>()
            .Where(s => s.Id == id)
            .Include(s => s.Messages.OrderBy(m => m.Ordinal))
            .FirstOrDefaultAsync(cancellationToken);
}
