using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RangeOps.Core.Telemetry;

/// <summary>Connects to the C sensor-sim over TCP and yields decoded readings
/// as an async stream.</summary>
public class TelemetryClient : ITelemetrySource
{
    private static string SimHost =>
        Environment.GetEnvironmentVariable("SIM_HOST") is { Length: > 0 } h ? h : "127.0.0.1";

    private static int SimPort =>
        int.TryParse(Environment.GetEnvironmentVariable("SIM_PORT"), out var p) ? p : 5555;

    public async IAsyncEnumerable<TelemetryReading> StreamAsync(
        string? host = null, int? port = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host ?? SimHost, port ?? SimPort, ct);
        await using var stream = tcp.GetStream();
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break; // sim closed the connection
            if (TelemetryReading.Parse(line) is { } reading)
                yield return reading;
        }
    }
}
