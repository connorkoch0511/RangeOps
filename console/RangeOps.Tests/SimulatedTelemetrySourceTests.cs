using RangeOps.Core.Telemetry;
using Xunit;

namespace RangeOps.Tests;

public class SimulatedTelemetrySourceTests
{
    [Fact]
    public async Task Stream_ClimbsAndInjectsADropout()
    {
        // Zero interval so the ~90-sample window runs instantly.
        var source = new SimulatedTelemetrySource(TimeSpan.Zero);
        using var cts = new CancellationTokenSource();

        var readings = new List<TelemetryReading>();
        await foreach (var r in source.StreamAsync(ct: cts.Token))
        {
            readings.Add(r);
            if (readings.Count >= 90) cts.Cancel(); // ~18 s covers the dropout window
        }

        Assert.True(readings.Count >= 90);
        Assert.Contains(readings, r => r.LinkDropout);        // a dropout was injected
        Assert.Contains(readings, r => !r.LinkDropout);       // and normal samples too
        Assert.True(readings[^1].AltitudeFt > readings[0].AltitudeFt); // the aircraft climbed
    }
}
