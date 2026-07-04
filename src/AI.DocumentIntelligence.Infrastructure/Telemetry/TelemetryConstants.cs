namespace AI.DocumentIntelligence.Infrastructure.Telemetry;

/// <summary>
/// Public constants for the OpenTelemetry instrumentation scope names used by the
/// Infrastructure layer.  The API composition root uses these names to register the
/// activity source and meter with the OTel provider builders.
///
/// The actual <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> instances are in
/// <see cref="InfrastructureActivitySource"/> (internal).
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// Name of the OpenTelemetry ActivitySource for Infrastructure traces.
    /// Register with <c>tracerProviderBuilder.AddSource(TelemetryConstants.ActivitySourceName)</c>.
    /// </summary>
    public const string ActivitySourceName = InfrastructureActivitySource.ActivitySourceName;

    /// <summary>
    /// Name of the OpenTelemetry Meter for Infrastructure metrics.
    /// Register with <c>meterProviderBuilder.AddMeter(TelemetryConstants.MeterName)</c>.
    /// </summary>
    public const string MeterName = InfrastructureActivitySource.MeterName;
}
