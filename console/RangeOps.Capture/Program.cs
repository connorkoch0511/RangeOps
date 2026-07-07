// Headless telemetry capture: connects to the sensor-sim and records a test run
// to Postgres -- the same CaptureService the desktop console uses, without a GUI.
//
//   rangeops-capture <runId> [maxSamples]
//
// Ctrl-C stops early. If maxSamples is given, capture stops after that many.
using RangeOps.Core.Telemetry;

if (args.Length < 1 || !int.TryParse(args[0], out var runId))
{
    Console.Error.WriteLine("usage: rangeops-capture <runId> [maxSamples]");
    return 1;
}
int? maxSamples = args.Length > 1 && int.TryParse(args[1], out var m) ? m : null;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var service = new CaptureService();
int seen = 0;

Console.WriteLine($"Capturing run #{runId}" +
                  (maxSamples is { } cap ? $" (up to {cap} samples)" : "") + "…");

var result = await service.CaptureAsync(runId, cts.Token, onSample: r =>
{
    seen++;
    Console.WriteLine(
        $"  alt={r.AltitudeFt,8:F0} ft   ias={r.AirspeedKt,5:F0} kt   " +
        $"vs={r.VerticalSpeedFpm,6:F0} fpm   {(r.LinkDropout ? "LINK DROPOUT" : "")}");
    if (maxSamples is { } limit && seen >= limit) cts.Cancel();
});

Console.WriteLine($"Done: {result.Samples} samples, {result.Dropouts} link dropouts " +
                  $"→ run #{runId} marked {result.Verdict}.");
return 0;
