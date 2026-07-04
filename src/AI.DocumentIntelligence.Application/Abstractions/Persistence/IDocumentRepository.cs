using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>
/// Repository contract for the <see cref="Document"/> aggregate.
/// Extends the generic <see cref="IRepository{T}"/> with owner-scoped retrieval
/// needed by document management handlers.
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Returns all documents owned by the specified user, or an empty list when none exist.
    /// </summary>
    /// <param name="ownerId">The owner's user identifier.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    public Task<IReadOnlyList<Document>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
