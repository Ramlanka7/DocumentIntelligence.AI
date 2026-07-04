using AI.DocumentIntelligence.Application.Abstractions.AI;
using AI.DocumentIntelligence.Application.Abstractions.Search;
using AI.DocumentIntelligence.Application.Contracts.Search;
using AI.DocumentIntelligence.Domain.Common;
using AI.DocumentIntelligence.Domain.Entities;
using AI.DocumentIntelligence.Domain.Enums;
using AI.DocumentIntelligence.Domain.ValueObjects;
using AI.DocumentIntelligence.Persistence;
using AI.DocumentIntelligence.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AI.DocumentIntelligence.Tests.Integration.Search;

/// <summary>
/// Integration tests that prove <see cref="AI.DocumentIntelligence.Persistence.Search.PgVectorSearchService"/>
/// performs correct cosine nearest-neighbour retrieval against a real pgvector database.
///
/// Container: <c>pgvector/pgvector:pg17</c> — the same image declared in docker-compose.yml.
/// Schema is created via EF Core <c>EnsureCreatedAsync</c> (no migration runner needed for tests).
/// The service is resolved via DI as <see cref="ISearchService"/>.
/// </summary>
[Collection("Docker")]
public sealed class PgVectorSearchServiceTests : IAsyncLifetime
{
    // ---- Testcontainer setup ----

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg17")
        .WithDatabase("docint_pgvector_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test")
        .Build();

    // Resolved after the container starts.
    private ISearchService _searchService = null!;
    private ServiceProvider _serviceProvider = null!;

    // ---- IAsyncLifetime ----

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();

        // Minimal configuration: just the connection string and no AzureSearch endpoint
        // so that AddPersistence selects PgVectorSearchService automatically.
        var configValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            // AzureSearch:Endpoint intentionally left blank → triggers PgVectorSearchService.
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();

        // Register the real AppDbContext targeting the test container.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddLogging();

        // Add Persistence layer services (registers PgVectorSearchService as ISearchService
        // because AzureSearch:Endpoint is blank).
        services.AddPersistence(configuration);

        // Replace ISearchService with PgVectorSearchService — Persistence already does this
        // when AzureSearch:Endpoint is missing, but we also need to provide IEmbeddingService
        // (PgVectorSearchService injects it to embed queries for SearchAsync).
        services.AddSingleton<IEmbeddingService, FixedVectorEmbeddingService>();

        _serviceProvider = services.BuildServiceProvider();

        // Apply the EF Core schema to the test DB.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Resolve the registered ISearchService (will be PgVectorSearchService).
        _searchService = _serviceProvider.GetRequiredService<ISearchService>();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ---- Helpers ----

    /// <summary>
    /// Seeds a <see cref="Document"/> row so the FK constraint on document_chunks is satisfied
    /// and the service can resolve document names from search results.
    /// </summary>
    private async Task<Document> SeedDocumentAsync(string fileName = "test.pdf")
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var doc = Document.Create(
            ownerId: Guid.NewGuid(),
            metadata: new FileMetadata(fileName, SizeBytes: 1024, PageCount: 5, ContentType: "application/pdf"),
            type: DocumentType.Pdf,
            storagePath: $"/uploads/{fileName}");

        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        return doc;
    }

    // ---- Tests ----

    [Fact]
    public async Task IndexAsync_PersistsChunks_IntoDocumentChunksTable()
    {
        // Arrange
        var doc = await SeedDocumentAsync("index-test.pdf");
        var chunks = new List<SearchableChunk>
        {
            new(doc.Id, doc.Metadata.FileName, 0, "First chunk content",  1, "§1", UnitVector(1536, 0)),
            new(doc.Id, doc.Metadata.FileName, 1, "Second chunk content", 1, "§2", UnitVector(1536, 1)),
            new(doc.Id, doc.Metadata.FileName, 2, "Third chunk content",  2, "§3", UnitVector(1536, 2)),
        };

        // Act
        var result = await _searchService.IndexAsync(chunks);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.DocumentChunks
            .Where(c => c.DocumentId == doc.Id)
            .ToListAsync();

        stored.Should().HaveCount(3);
        stored.Select(c => c.Content).Should().BeEquivalentTo(
        [
            "First chunk content",
            "Second chunk content",
            "Third chunk content",
        ]);
    }

    [Fact]
    public async Task IndexAsync_ReIngest_DeletesOldChunksBeforeInsert()
    {
        // Arrange: initial ingest.
        var doc = await SeedDocumentAsync("reingest-test.pdf");
        var initialChunks = new List<SearchableChunk>
        {
            new(doc.Id, doc.Metadata.FileName, 0, "Old chunk A", 1, "§1", UnitVector(1536, 0)),
            new(doc.Id, doc.Metadata.FileName, 1, "Old chunk B", 1, "§2", UnitVector(1536, 1)),
        };
        await _searchService.IndexAsync(initialChunks);

        // Re-ingest with different content.
        var newChunks = new List<SearchableChunk>
        {
            new(doc.Id, doc.Metadata.FileName, 0, "New chunk X", 1, "§1", UnitVector(1536, 0)),
        };

        // Act
        var result = await _searchService.IndexAsync(newChunks);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.DocumentChunks
            .Where(c => c.DocumentId == doc.Id)
            .ToListAsync();

        stored.Should().HaveCount(1, "old chunks must be replaced on re-ingest");
        stored[0].Content.Should().Be("New chunk X");
    }

    [Fact]
    public async Task SearchAsync_ReturnsMostSimilarChunks_OrderedByCosineSimilarity()
    {
        // Arrange: three chunks with orthogonal unit-vector embeddings (1536 dimensions).
        // Query vector == chunk A embedding → cosine distance 0 → similarity score 1.0.
        // Chunks B and C are orthogonal → cosine distance 1 → similarity score 0.5.
        var doc = await SeedDocumentAsync("search-order-test.pdf");

        var chunkAEmbedding = UnitVector(1536, 0);   // along dim 0
        var chunkBEmbedding = UnitVector(1536, 1);   // along dim 1 (orthogonal)
        var chunkCEmbedding = UnitVector(1536, 2);   // along dim 2 (orthogonal)

        var chunks = new List<SearchableChunk>
        {
            new(doc.Id, doc.Metadata.FileName, 0, "Chunk A — very relevant",  1, "§A", chunkAEmbedding),
            new(doc.Id, doc.Metadata.FileName, 1, "Chunk B — less relevant",  1, "§B", chunkBEmbedding),
            new(doc.Id, doc.Metadata.FileName, 2, "Chunk C — least relevant", 2, "§C", chunkCEmbedding),
        };
        await _searchService.IndexAsync(chunks);

        // Configure the stub so the query embedding equals chunk A's embedding.
        FixedVectorEmbeddingService.NextQueryEmbedding = chunkAEmbedding;

        var request = new SearchRequest(
            Query: "relevant topic",
            DocumentIds: [doc.Id],
            TopK: 3);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hits = result.Value;
        hits.Should().HaveCount(3);

        // Chunk A is identical to the query → distance ≈ 0 → score ≈ 1.0.
        hits[0].Content.Should().Be("Chunk A — very relevant");
        hits[0].Score.Should().BeApproximately(1.0, precision: 0.01,
            because: "cosine distance 0 maps to similarity score 1");

        // All subsequent hits must have lower or equal scores.
        hits[1].Score.Should().BeLessOrEqualTo(hits[0].Score);
        hits[2].Score.Should().BeLessOrEqualTo(hits[1].Score);

        // All hits carry citation-ready metadata.
        hits.Should().AllSatisfy(h =>
        {
            h.DocumentId.Should().Be(doc.Id);
            h.DocumentName.Should().Be(doc.Metadata.FileName);
            h.PageNumber.Should().BeGreaterThan(0);
            h.ParagraphReference.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task SearchAsync_FilteredToDocumentIds_ExcludesOtherDocuments()
    {
        // Arrange: two documents, each with one chunk using the same embedding.
        var docA = await SeedDocumentAsync("doc-filter-a.pdf");
        var docB = await SeedDocumentAsync("doc-filter-b.pdf");

        var embedding = UnitVector(1536, 0);

        await _searchService.IndexAsync(
        [
            new SearchableChunk(docA.Id, docA.Metadata.FileName, 0, "Doc A content", 1, "§1", embedding),
        ]);
        await _searchService.IndexAsync(
        [
            new SearchableChunk(docB.Id, docB.Metadata.FileName, 0, "Doc B content", 1, "§1", embedding),
        ]);

        FixedVectorEmbeddingService.NextQueryEmbedding = embedding;

        var request = new SearchRequest(
            Query: "query",
            DocumentIds: [docA.Id],   // only docA
            TopK: 10);

        // Act
        var result = await _searchService.SearchAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].DocumentId.Should().Be(docA.Id);
        result.Value[0].Content.Should().Be("Doc A content");
    }

    [Fact]
    public async Task DeleteByDocumentAsync_RemovesAllChunksForDocument()
    {
        // Arrange
        var doc = await SeedDocumentAsync("delete-test.pdf");
        await _searchService.IndexAsync(
        [
            new SearchableChunk(doc.Id, doc.Metadata.FileName, 0, "Chunk 0", 1, "§1", UnitVector(1536, 0)),
            new SearchableChunk(doc.Id, doc.Metadata.FileName, 1, "Chunk 1", 1, "§2", UnitVector(1536, 1)),
        ]);

        // Act
        var result = await _searchService.DeleteByDocumentAsync(doc.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remaining = await db.DocumentChunks
            .Where(c => c.DocumentId == doc.Id)
            .CountAsync();

        remaining.Should().Be(0);
    }

    [Fact]
    public async Task IndexAsync_WithEmptyList_ReturnsSuccess_WithoutTouchingDb()
    {
        var result = await _searchService.IndexAsync([]);

        result.IsSuccess.Should().BeTrue();
    }

    // ---- Unit-vector helper ----

    /// <summary>
    /// Builds a unit vector of <paramref name="dimension"/> floats with all zeros except
    /// a 1.0 at <paramref name="hotIndex"/>. Used to create orthogonal embeddings for
    /// deterministic cosine-similarity testing.
    /// </summary>
    private static IReadOnlyList<float> UnitVector(int dimension, int hotIndex)
    {
        var vec = new float[dimension];
        vec[hotIndex] = 1.0f;
        return vec;
    }
}

/// <summary>
/// Stub <see cref="IEmbeddingService"/> that returns a configurable fixed vector for every input.
/// Tests set <see cref="NextQueryEmbedding"/> before calling <c>SearchAsync</c>.
/// </summary>
internal sealed class FixedVectorEmbeddingService : IEmbeddingService
{
    /// <summary>The embedding vector returned for the next <see cref="GenerateEmbeddingAsync"/> call.</summary>
    public static IReadOnlyList<float> NextQueryEmbedding { get; set; } =
        new float[1536]; // zeros by default

    public Task<Result<IReadOnlyList<float>>> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(NextQueryEmbedding));

    public Task<Result<IReadOnlyList<IReadOnlyList<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReadOnlyList<float>> results =
            inputs.Select(_ => NextQueryEmbedding).ToList();
        return Task.FromResult(Result.Success(results));
    }
}
