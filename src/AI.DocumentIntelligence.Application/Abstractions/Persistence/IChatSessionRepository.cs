using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Application.Abstractions.Persistence;

/// <summary>
/// Dedicated repository for <see cref="ChatSession"/> that exposes queries which eager-load
/// the <c>Messages</c> navigation so that handlers receive fully-populated sessions.
/// The generic <see cref="IRepository{T}"/> does not perform any <c>Include</c>, so list and
/// detail queries for chat sessions must go through this contract.
/// </summary>
public interface IChatSessionRepository
{
    /// <summary>
    /// Returns all chat sessions belonging to <paramref name="ownerId"/>, with their
    /// <c>Messages</c> navigation loaded and messages ordered by <c>Ordinal</c>.
    /// </summary>
    public Task<IReadOnlyList<ChatSession>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the chat session with the given <paramref name="id"/>, with its
    /// <c>Messages</c> navigation loaded and messages ordered by <c>Ordinal</c>, or
    /// <see langword="null"/> when not found.
    /// </summary>
    public Task<ChatSession?> GetByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
