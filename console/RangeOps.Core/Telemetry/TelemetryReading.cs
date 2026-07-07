using System.Text.Json;

namespace RangeOps.Core.Telemetry;

/// <summary>A single decoded telemetry reading from the sensor-sim.</summary>
public readonly record struct TelemetryReading(
    double AltitudeFt,
    double AirspeedKt,
    double VerticalSpeedFpm,
    bool LinkDropout)
{
    /// <summary>
    /// Parse one line of the sim's wire format:
    /// {"alt_ft":123.4,"airspeed_kt":250.0,"vs_fpm":1800.0,"link_dropout":false}
    /// Returns null for blank or malformed lines (defensive against partial reads).
    /// </summary>
    public static TelemetryReading? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var r = doc.RootElement;
            return new TelemetryReading(
                r.GetProperty("alt_ft").GetDouble(),
                r.GetProperty("airspeed_kt").GetDouble(),
                r.GetProperty("vs_fpm").GetDouble(),
                r.GetProperty("link_dropout").GetBoolean());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}
