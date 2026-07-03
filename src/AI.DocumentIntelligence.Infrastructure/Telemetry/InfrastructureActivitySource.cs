using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AI.DocumentIntelligence.Infrastructure.Telemetry;

/// <summary>
/// Central registry for the custom OpenTelemetry <see cref="System.Diagnostics.ActivitySource"/>
/// and <see cref="System.Diagnostics.Metrics.Meter"/> used throughout the Infrastructure layer.
///
/// All AI/search operations that should produce traces or metrics must acquire their
/// <see cref="Activity"/> instances from <see cref="Source"/> and record measurements via the
/// pre-built instruments below.  The names registered here must be added to the OTel tracer/meter
/// provider in <c>ObservabilityExtensions</c> so the SDK picks up the spans/metrics.
/// </summary>
internal static class InfrastructureActivitySource
{
    /// <summary>
    /// The ActivitySource name (also the OTel instrumentation scope name).
    /// Must match the name passed to <c>AddSource</c> in the tracer provider builder.
    /// </summary>
    internal const string ActivitySourceName = "AI.DocumentIntelligence.Infrastructure";

    /// <summary>
    /// The Meter name used for custom metrics.
    /// Must match the name passed to <c>AddMeter</c> in the meter provider builder.
    /// </summary>
    internal const string MeterName = "AI.DocumentIntelligence.Infrastructure";

    // --- Activity source -----------------------------------------------------------------------

    /// <summary>Shared ActivitySource for custom Infrastructure spans.</summary>
    internal static readonly ActivitySource Source = new(ActivitySourceName);

    // --- Meter + instruments -------------------------------------------------------------------

    private static readonly Meter InternalMeter = new(MeterName);

    /// <summary>Total number of AI completion calls dispatched (by provider and model).</summary>
    internal static readonly Counter<long> AiCompletionRequests =
        InternalMeter.CreateCounter<long>(
            "ai.completion.requests",
            unit: "{requests}",
            description: "Total number of AI completion requests dispatched.");

    /// <summary>End-to-end latency (ms) of each AI completion call.</summary>
    internal static readonly Histogram<double> AiCompletionDurationMs =
        InternalMeter.CreateHistogram<double>(
            "ai.completion.duration_ms",
            unit: "ms",
            description: "End-to-end duration of AI completion calls in milliseconds.");

    /// <summary>Total prompt + completion tokens consumed per AI call.</summary>
    internal static readonly Counter<long> AiTokensConsumed =
        InternalMeter.CreateCounter<long>(
            "ai.tokens.consumed",
            unit: "{tokens}",
            description: "Total LLM tokens consumed (prompt + completion).");

    /// <summary>Estimated USD cost for each AI completion call.</summary>
    internal static readonly Histogram<double> AiCompletionCostUsd =
        InternalMeter.CreateHistogram<double>(
            "ai.completion.cost_usd",
            unit: "USD",
            description: "Estimated cost in USD for each AI completion call.");

    /// <summary>Total number of Azure AI Search requests dispatched.</summary>
    internal static readonly Counter<long> SearchRequests =
        InternalMeter.CreateCounter<long>(
            "search.requests",
            unit: "{requests}",
            description: "Total number of Azure AI Search requests dispatched.");

    /// <summary>End-to-end latency (ms) of each Azure AI Search call.</summary>
    internal static readonly Histogram<double> SearchDurationMs =
        InternalMeter.CreateHistogram<double>(
            "search.duration_ms",
            unit: "ms",
            description: "End-to-end duration of Azure AI Search calls in milliseconds.");
}
