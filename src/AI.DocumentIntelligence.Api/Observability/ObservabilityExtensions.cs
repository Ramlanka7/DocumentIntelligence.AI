using System.Text.Json;
using System.Text.Json.Serialization;
using AI.DocumentIntelligence.Infrastructure.Telemetry;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace AI.DocumentIntelligence.Api.Observability;

/// <summary>
/// Extension methods that wire up the full observability stack for the API:
/// Serilog structured logging, OpenTelemetry traces + metrics, and health check response formatting.
/// Each telemetry feature degrades gracefully when its backing service (OTLP endpoint, Application
/// Insights connection string) is not present in configuration.
/// </summary>
internal static class ObservabilityExtensions
{
    private const string ServiceName = "document-intelligence-api";
    private const string ServiceVersion = "1.0.0";

    /// <summary>Cached serializer options for the health check JSON response writer.</summary>
    private static readonly JsonSerializerOptions HealthCheckJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Configures Serilog as the logging provider.  Reads sink/level overrides from the
    /// <c>Serilog</c> configuration section; enriches every event with machine name, environment,
    /// thread ID, and correlation ID (pushed by <see cref="Middleware.CorrelationIdMiddleware"/>).
    /// </summary>
    internal static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Bootstrap logger: active before the host is fully built (catches startup errors).
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)  // appsettings Serilog section
                .ReadFrom.Services(services)                     // ILogEventEnricher registrations
                .Enrich.FromLogContext()                         // picks up CorrelationId etc.
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId();
        });

        return builder;
    }

    /// <summary>
    /// Registers OpenTelemetry tracing and metrics.
    /// <list type="bullet">
    ///   <item>Always enables ASP.NET Core, outbound HTTP, and EF Core instrumentation.</item>
    ///   <item>Includes custom AI/search spans from <see cref="TelemetryConstants"/>.</item>
    ///   <item>Adds OTLP exporter only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> env var is set.</item>
    ///   <item>Adds Azure Monitor exporter only when <c>ApplicationInsights:ConnectionString</c> is set.</item>
    /// </list>
    /// </summary>
    internal static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resource = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: ServiceName,
                serviceVersion: ServiceVersion,
                autoGenerateServiceInstanceId: true)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] =
                    configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
            });

        // Read once; reused by both the tracing and metrics builder lambdas.
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var appInsightsCs = configuration["ApplicationInsights:ConnectionString"];

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resource)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        // Exclude health check endpoints from traces to reduce noise.
                        opts.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health",
                                StringComparison.OrdinalIgnoreCase);
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(TelemetryConstants.ActivitySourceName);

                // OTLP exporter — active when the standard env var is set.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                        opts.Endpoint = new Uri(otlpEndpoint));
                }

                // Azure Monitor (Application Insights) — active when connection string is set.
                if (!string.IsNullOrWhiteSpace(appInsightsCs))
                {
                    tracing.AddAzureMonitorTraceExporter(opts =>
                        opts.ConnectionString = appInsightsCs);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resource)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(TelemetryConstants.MeterName);

                // OTLP metrics exporter.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts =>
                        opts.Endpoint = new Uri(otlpEndpoint));
                }

                // Azure Monitor metrics exporter.
                if (!string.IsNullOrWhiteSpace(appInsightsCs))
                {
                    metrics.AddAzureMonitorMetricExporter(opts =>
                        opts.ConnectionString = appInsightsCs);
                }
            });

        return services;
    }

    /// <summary>
    /// Writes a structured JSON health-check response that includes per-component status,
    /// description, and wall-clock duration.  Used by the <c>/health</c>,
    /// <c>/health/live</c>, and <c>/health/ready</c> endpoints.
    /// </summary>
    internal static Task WriteHealthCheckResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration_ms = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null,
            }),
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, HealthCheckJsonOptions));
    }
}
