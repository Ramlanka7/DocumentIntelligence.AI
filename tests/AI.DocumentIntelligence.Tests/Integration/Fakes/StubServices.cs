using AI.DocumentIntelligence.Application.Abstractions;
using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Storage;
using AI.DocumentIntelligence.Domain.Common;

namespace AI.DocumentIntelligence.Tests.Integration.Fakes;

/// <summary>No-op audit service — records nothing, never throws.</summary>
public sealed class StubAuditService : IAuditService
{
    public Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>
/// No-op embedding service — returns a zero vector so no real Azure call is made.
/// </summary>
public sealed class StubEmbeddingService : IEmbeddingService
{
    private static readonly IReadOnlyList<float> ZeroVector = new float[1536];

    public Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(ZeroVector));

    public Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReadOnlyList<float>> results = inputs.Select(_ => ZeroVector).ToList();
        return Task.FromResult(Result.Success(results));
    }
}

/// <summary>In-memory file storage that discards files — safe for integration tests.</summary>
public sealed class StubFileStorage : IFileStorage
{
    public Task<Result<string>> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success($"stub/{Guid.NewGuid():N}_{fileName}"));

    public Task<Result<Stream>> GetAsync(
        string storageKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success<Stream>(new MemoryStream()));

    public Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}

