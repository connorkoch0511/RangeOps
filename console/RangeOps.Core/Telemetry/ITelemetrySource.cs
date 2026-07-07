namespace RangeOps.Core.Telemetry;

/// <summary>A source of telemetry readings. The real implementation is
/// <see cref="TelemetryClient"/> (TCP to the sensor-sim); tests supply an
/// in-memory source so the capture pipeline can be exercised without a socket.</summary>
public interface ITelemetrySource
{
    IAsyncEnumerable<TelemetryReading> StreamAsync(
        string? host = null, int? port = null, CancellationToken ct = default);
}
