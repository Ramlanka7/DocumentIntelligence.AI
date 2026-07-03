using AI.DocumentIntelligence.Application.Abstractions.Persistence;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>
/// No-op unit-of-work for integration tests. Because the in-memory repositories persist
/// immediately on Add/Remove, the SaveChangesAsync call is a no-op.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    IRepository<T> IUnitOfWork.Repository<T>() =>
        throw new NotSupportedException(
            "Direct repository access via IUnitOfWork is not needed in these tests. " +
            "Inject the typed repository directly instead.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(1); // Simulate one row affected.
}

