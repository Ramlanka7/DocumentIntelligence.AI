using System.Diagnostics;
using Serilog.Context;

namespace AI.DocumentIntelligence.Api.Middleware;

/// <summary>
/// Reads an inbound <c>X-Correlation-Id</c> header (or generates a new one) and:
/// <list type="bullet">
///   <item>Stores it in <c>HttpContext.Items</c> under the key <see cref="CorrelationIdKey"/>.</item>
///   <item>Echoes it back in the <c>X-Correlation-Id</c> response header.</item>
///   <item>Pushes it as a Serilog <see cref="LogContext"/> property named <c>CorrelationId</c>.</item>
///   <item>Adds it as a tag on <see cref="Activity.Current"/> for OpenTelemetry trace correlation.</item>
/// </list>
/// Must be registered early in the pipeline — before Serilog's request-logging middleware —
/// so that all subsequent log entries for the request carry the correlation ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The key used to store/retrieve the correlation ID in <c>HttpContext.Items</c>.</summary>
    public const string CorrelationIdKey = "CorrelationId";

    /// <summary>The HTTP header name used to propagate the correlation ID.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <inheritdoc cref="RequestDelegate"/>
    public async Task InvokeAsync(HttpContext context)
    {
        var inboundCorrelationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        var correlationId = Guid.TryParse(inboundCorrelationId, out var parsed)
            ? parsed.ToString("D")
            : Guid.NewGuid().ToString("D");

        // Surface on the response so callers can correlate client-side.
        context.Items[CorrelationIdKey] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Tag the current OTel activity so the value appears in traces.
        Activity.Current?.SetTag("app.correlation_id", correlationId);

        // Push into Serilog's async log context so all log events for this
        // request automatically carry the correlation ID as a structured property.
        using (LogContext.PushProperty(CorrelationIdKey, correlationId))
        {
            await next(context);
        }
    }
}
