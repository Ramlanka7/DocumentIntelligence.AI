using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AI.DocumentIntelligence.Tests.Integration.Database;

/// <summary>
/// Spins up an ephemeral PostgreSQL container via Testcontainers and verifies connectivity.
/// PostgreSQL holds relational state only — document chunks and embeddings live in
/// Azure AI Search, so there is no vector extension to exercise here.
/// </summary>
[Collection("Docker")]
public sealed class DatabaseContainerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
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
    public async Task Container_ConnectionString_IsWellFormed()
    {
        var connectionString = _container.GetConnectionString();

        connectionString.Should().Contain("Host=");
        connectionString.Should().Contain("Port=");
        connectionString.Should().Contain("Database=");
    }
}
