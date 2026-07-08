using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RangeOps.Core.Telemetry;

/// <summary>
/// Uses the real sensor-sim over TCP when it's reachable, and otherwise falls
/// back to the in-process <see cref="SimulatedTelemetrySource"/>. This lets the
/// console capture telemetry whether or not the external sim is running.
/// </summary>
public class FallbackTelemetrySource : ITelemetrySource
{
    private readonly TelemetryClient _tcp = new();
    private readonly SimulatedTelemetrySource _simulated = new();

    private static string SimHost =>
        Environment.GetEnvironmentVariable("SIM_HOST") is { Length: > 0 } h ? h : "127.0.0.1";

    private static int SimPort =>
        int.TryParse(Environment.GetEnvironmentVariable("SIM_PORT"), out var p) ? p : 5555;

    public async IAsyncEnumerable<TelemetryReading> StreamAsync(
        string? host = null, int? port = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var source = await SimReachableAsync(host ?? SimHost, port ?? SimPort, ct)
            ? (ITelemetrySource)_tcp
            : _simulated;

        await foreach (var reading in source.StreamAsync(host, port, ct))
            yield return reading;
    }

    /// <summary>Quick probe: can we open a TCP connection to the sim?</summary>
    private static async Task<bool> SimReachableAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var tcp = new TcpClient();
            var connect = tcp.ConnectAsync(host, port);
            var timeout = Task.Delay(500, ct);
            var finished = await Task.WhenAny(connect, timeout);
            return finished == connect && !connect.IsFaulted && tcp.Connected;
        }
        catch
        {
            return false;
        }
    }
}
