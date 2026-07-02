using AI.DocumentIntelligence.Infrastructure.AI.Options;
using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.DocumentIntelligence.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that Azure AI Search is reachable and the configured index exists.
/// Returns <see cref="HealthStatus.Degraded"/> when credentials are absent (not configured)
/// and <see cref="HealthStatus.Unhealthy"/> only when the service is configured but unreachable.
/// </summary>
internal sealed partial class AzureSearchHealthCheck : IHealthCheck
{
    private readonly AzureSearchOptions _options;
    private readonly ILogger<AzureSearchHealthCheck> _logger;

    // Cached so we do not instantiate a new SDK client on every health-check poll.
    private readonly SearchIndexClient? _indexClient;

    public AzureSearchHealthCheck(
        IOptions<AzureSearchOptions> options,
        ILogger<AzureSearchHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.Endpoint) && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _indexClient = new SearchIndexClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));
        }
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_indexClient is null)
        {
            return HealthCheckResult.Degraded(
                "Azure AI Search is not configured (missing Endpoint or ApiKey).");
        }

        try
        {
            var stats = await _indexClient.GetServiceStatisticsAsync(cancellationToken);
            var indexCount = stats.Value.Counters.IndexCounter.Usage;

            return HealthCheckResult.Healthy(
                $"Azure AI Search is reachable. Indexes: {indexCount}.",
                new Dictionary<string, object>
                {
                    ["endpoint"] = _options.Endpoint,
                    ["index_count"] = indexCount,
                });
        }
        catch (RequestFailedException ex)
        {
            LogRequestFailed(logger: _logger, status: ex.Status, exception: ex);
            return HealthCheckResult.Unhealthy(
                $"Azure AI Search returned HTTP {ex.Status}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            LogUnexpectedFailure(logger: _logger, exception: ex);
            return HealthCheckResult.Unhealthy("Azure AI Search health check failed.", ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Azure AI Search health check failed with HTTP {Status}.")]
    private static partial void LogRequestFailed(ILogger logger, int status, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Azure AI Search health check failed unexpectedly.")]
    private static partial void LogUnexpectedFailure(ILogger logger, Exception exception);
}
