using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AI.DocumentIntelligence.Tests.Integration.Database;

/// <summary>
/// Spins up an ephemeral PostgreSQL + pgvector container via Testcontainers and verifies
/// connectivity and extension availability.  These tests prove the container infrastructure
/// works and will be extended once the full EF Core persistence layer (T02) is in place.
/// </summary>
[Collection("Docker")]
public sealed class DatabaseContainerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("docint_test")
        .WithUsername("postgres")
        .WithPassword("postgres_test")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Container_StartsAndAcceptsConnections()
    {
        // Arrange
        var connectionString = _container.GetConnectionString();

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        connection.State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task Container_ExecutesSimpleQuery()
    {
        var connectionString = _container.GetConnectionString();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        var result = await command.ExecuteScalarAsync();

        result.Should().Be(1);
    }

    [Fact]
    public async Task Container_PgvectorExtension_CanBeCreated()
    {
        // Arrange: pgvector/pgvector image ships with the pgvector extension pre-installed.
        var connectionString = _container.GetConnectionString();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act: enable the pgvector extension.
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
        var act = async () => await create.ExecuteNonQueryAsync();

        // Assert: the extension installed without error.
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Container_VectorColumn_CanStoreAndRetrieve()
    {
        var connectionString = _container.GetConnectionString();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Enable pgvector.
        await using var ext = connection.CreateCommand();
        ext.CommandText = "CREATE EXTENSION IF NOT EXISTS vector";
        await ext.ExecuteNonQueryAsync();

        // Create a minimal table with a vector column (dimension 3 for test speed).
        await using var createTable = connection.CreateCommand();
        createTable.CommandText = """
            CREATE TEMP TABLE test_vectors (
                id   serial PRIMARY KEY,
                vec  vector(3)
            )
            """;
        await createTable.ExecuteNonQueryAsync();

        // Insert a vector.
        await using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO test_vectors (vec) VALUES ('[1,2,3]'::vector)";
        await insert.ExecuteNonQueryAsync();

        // Read it back.
        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT vec::text FROM test_vectors LIMIT 1";
        var stored = (string?)await select.ExecuteScalarAsync();

        stored.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Container_ConnectionString_IsWellFormed()
    {
        var connectionString = _container.GetConnectionString();

        connectionString.Should().Contain("Host=");
        connectionString.Should().Contain("Port=");
        connectionString.Should().Contain("Database=");
    }
}
