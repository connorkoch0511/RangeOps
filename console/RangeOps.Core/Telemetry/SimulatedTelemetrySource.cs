using System.Runtime.CompilerServices;

namespace RangeOps.Core.Telemetry;

/// <summary>
/// An in-process telemetry generator that mirrors the C sensor-sim: a climbing
/// test aircraft at 5 Hz with an injected data-link dropout (all channels held
/// stale) around t=8-14 s. Used so the console can capture without the external
/// sim running -- e.g. in a downloaded, standalone build.
/// </summary>
public class SimulatedTelemetrySource : ITelemetrySource
{
    private const int Hz = 5;
    private const double CruiseAltFt = 25000.0;
    private const double CruiseKt = 320.0;

    private readonly TimeSpan _interval;

    /// <param name="interval">Delay between samples. Defaults to 1/5 s (5 Hz);
    /// pass <see cref="TimeSpan.Zero"/> in tests to run without waiting.</param>
    public SimulatedTelemetrySource(TimeSpan? interval = null) =>
        _interval = interval ?? TimeSpan.FromMilliseconds(1000.0 / Hz);

    public async IAsyncEnumerable<TelemetryReading> StreamAsync(
        string? host = null, int? port = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var rng = new Random();
        double alt = 0.0, airspeed = 140.0;
        double rxAlt = 0.0, rxIas = 140.0, rxVs = 0.0; // last received (held on dropout)
        long tick = 0;
        const long dropoutStart = 8 * Hz;
        const long dropoutEnd = 14 * Hz;

        double Noise(double mag) => rng.NextDouble() * 2.0 * mag - mag;

        while (!ct.IsCancellationRequested)
        {
            double vs;
            if (alt < CruiseAltFt)
            {
                vs = 2000.0 + Noise(120.0);
                alt += vs / 60.0 / Hz;
                if (airspeed < CruiseKt) airspeed += 0.4;
            }
            else
            {
                alt = CruiseAltFt;
                vs = Noise(80.0);
            }
            airspeed += Noise(1.5);

            bool dropout = tick >= dropoutStart && tick < dropoutEnd;
            if (!dropout)
            {
                rxAlt = alt + Noise(15.0);
                rxIas = airspeed;
                rxVs = vs;
            }

            yield return new TelemetryReading(rxAlt, rxIas, rxVs, dropout);
            tick++;

            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { yield break; }
        }
    }
}
