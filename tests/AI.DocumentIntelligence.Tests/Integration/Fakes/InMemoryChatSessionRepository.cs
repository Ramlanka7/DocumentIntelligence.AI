using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>
/// In-memory <see cref="IChatSessionRepository"/> for unit tests. Because domain
/// <see cref="ChatSession.AddMessage"/> populates the in-memory <c>Messages</c> collection directly,
/// no eager-loading is needed here — the fake simply returns the stored aggregates.
/// </summary>
public sealed class InMemoryChatSessionRepository : IChatSessionRepository
{
    private readonly List<ChatSession> _sessions = [];

    /// <summary>Adds a session to the in-memory store.</summary>
    public void Add(ChatSession session) => _sessions.Add(session);

    public Task<IReadOnlyList<ChatSession>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChatSession>>(
            _sessions.Where(s => s.OwnerId == ownerId).ToList());

    public Task<ChatSession?> GetByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));
}
