using System.Linq.Expressions;
using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Entities;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>Thread-safe in-memory document store for integration tests.</summary>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _documents = [];

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Document>>(_documents.AsReadOnly());

    public Task<IReadOnlyList<Document>> FindAsync(
        Expression<Func<Document, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Document>>(
            _documents.AsQueryable().Where(predicate).ToList().AsReadOnly());

    public Task AddAsync(Document entity, CancellationToken cancellationToken = default)
    {
        _documents.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(Document entity)
    {
        // In-memory store: entity is already the live reference, no action needed.
    }

    public void Remove(Document entity) => _documents.Remove(entity);

    public Task<IReadOnlyList<Document>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Document>>(
            _documents.Where(d => d.OwnerId == ownerId).ToList().AsReadOnly());

    // ---- Test helpers ----

    public void Seed(IEnumerable<Document> documents) => _documents.AddRange(documents);

    public IReadOnlyList<Document> All => _documents.AsReadOnly();
}
