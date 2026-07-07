using RangeOps.Core.Data;
using RangeOps.Core.Models;

namespace RangeOps.Core.Telemetry;

public readonly record struct CaptureResult(int Samples, int Dropouts)
{
    // A run whose telemetry had data-link dropouts is flagged FAIL -- the
    // captured data is incomplete/unreliable and the run needs review or re-fly.
    public string Verdict => Dropouts > 0 ? "FAIL" : "PASS";
}

/// <summary>
/// Captures a telemetry stream from the sensor-sim into a test run: marks the
/// run RUNNING, persists every sample via EF Core, and on completion marks the
/// run PASS/FAIL depending on whether any data-link dropouts were detected.
///
/// Shared by the desktop console (live UI) and the headless capture CLI, so the
/// exact same pipeline is exercised by the GUI, the CLI, and the tests.
/// </summary>
public class CaptureService
{
    private readonly ITelemetrySource _client;
    private readonly Func<RangeOpsContext> _contextFactory;

    public CaptureService(ITelemetrySource? client = null,
                          Func<RangeOpsContext>? contextFactory = null)
    {
        _client = client ?? new TelemetryClient();
        _contextFactory = contextFactory ?? (() => new RangeOpsContext());
    }

    public async Task<CaptureResult> CaptureAsync(
        int runId,
        CancellationToken ct,
        Action<TelemetryReading>? onSample = null,
        string? simHost = null,
        int? simPort = null)
    {
        await SetRunStateAsync(runId, "RUNNING", started: true);

        int samples = 0, dropouts = 0;
        try
        {
            await using var db = _contextFactory();
            await foreach (var r in _client.StreamAsync(simHost, simPort, ct))
            {
                db.TelemetrySamples.Add(new TelemetrySample
                {
                    TestRunId = runId,
                    SampleTs = DateTime.UtcNow,
                    AltitudeFt = (float)r.AltitudeFt,
                    AirspeedKt = (float)r.AirspeedKt,
                    VerticalSpeedFpm = (float)r.VerticalSpeedFpm,
                    LinkDropout = r.LinkDropout,
                });
                await db.SaveChangesAsync(ct);
                samples++;
                if (r.LinkDropout) dropouts++;
                onSample?.Invoke(r);
            }
        }
        catch (OperationCanceledException) { /* normal stop */ }

        var result = new CaptureResult(samples, dropouts);
        await SetRunStateAsync(runId, result.Verdict, ended: true,
            notes: $"{samples} samples, {dropouts} link dropouts");
        return result;
    }

    private async Task SetRunStateAsync(int runId, string status,
        bool started = false, bool ended = false, string? notes = null)
    {
        await using var db = _contextFactory();
        var run = await db.TestRuns.FindAsync(runId);
        if (run is null) throw new InvalidOperationException($"Test run {runId} not found.");
        run.Status = status;
        if (started) run.StartedAt = DateTime.UtcNow;
        if (ended) run.EndedAt = DateTime.UtcNow;
        if (notes is not null) run.Notes = notes;
        await db.SaveChangesAsync();
    }
}
