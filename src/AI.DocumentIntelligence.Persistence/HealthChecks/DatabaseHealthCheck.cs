using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AI.DocumentIntelligence.Persistence.HealthChecks;

/// <summary>
/// Verifies that the PostgreSQL database is reachable by opening a connection and executing
/// <c>SELECT 1</c>.  Returns <see cref="HealthStatus.Degraded"/> (not <c>Unhealthy</c>) when the
/// connection string is absent — this is expected while T02 (full EF Core persistence) is pending.
/// </summary>
internal sealed partial class DatabaseHealthCheck(
    IConfiguration configuration,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    private const string ConnectionStringKey = "ConnectionStrings:DefaultConnection";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration[ConnectionStringKey];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Degraded(
                "Database connection string is not configured (pending T02 — EF Core persistence).");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                "PostgreSQL is reachable.",
                new Dictionary<string, object> { ["server"] = connection.Host ?? "unknown" });
        }
        catch (NpgsqlException ex)
        {
            LogNpgsqlFailure(logger, ex);
            return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", ex);
        }
        catch (Exception ex)
        {
            LogUnexpectedFailure(logger, ex);
            return HealthCheckResult.Unhealthy("Database health check failed unexpectedly.", ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database health check failed with a PostgreSQL exception.")]
    private static partial void LogNpgsqlFailure(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database health check failed unexpectedly.")]
    private static partial void LogUnexpectedFailure(ILogger logger, Exception exception);
}
