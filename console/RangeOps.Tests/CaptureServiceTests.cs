using System.Runtime.CompilerServices;
using RangeOps.Core.Data;
using RangeOps.Core.Models;
using RangeOps.Core.Telemetry;
using Xunit;

namespace RangeOps.Tests;

/// <summary>
/// Exercises the capture pipeline (CaptureService → EF Core → Postgres) with an
/// in-memory telemetry source, so no sensor-sim socket is needed. Requires the
/// docker-compose Postgres to be running.
/// </summary>
public class CaptureServiceTests
{
    /// <summary>A fake telemetry source that replays a fixed set of readings.</summary>
    private sealed class FakeSource : ITelemetrySource
    {
        private readonly IReadOnlyList<TelemetryReading> _readings;
        public FakeSource(IReadOnlyList<TelemetryReading> readings) => _readings = readings;

        public async IAsyncEnumerable<TelemetryReading> StreamAsync(
            string? host = null, int? port = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var r in _readings)
            {
                ct.ThrowIfCancellationRequested();
                yield return r;
                await Task.Yield();
            }
        }
    }

    [Fact]
    public async Task Capture_WithFaults_PersistsSamplesAndMarksRunFail()
    {
        var marker = $"__cap_{Guid.NewGuid():N}";
        int runId;
        await using (var db = new RangeOpsContext())
        {
            var mission = new Mission
            {
                Name = marker, Aircraft = "F-16C",
                ScheduledStart = DateTime.UtcNow, ScheduledEnd = DateTime.UtcNow.AddHours(1),
                Status = "ACTIVE",
            };
            db.Missions.Add(mission);
            await db.SaveChangesAsync();
            var run = new TestRun { MissionId = mission.Id, Name = "Capture", Status = "PENDING" };
            db.TestRuns.Add(run);
            await db.SaveChangesAsync();
            runId = run.Id;
        }

        var readings = new[]
        {
            new TelemetryReading(1000, 250, 2000, false),
            new TelemetryReading(1200, 255, 2000, true),  // fault
            new TelemetryReading(1400, 260, 2000, true),  // fault
            new TelemetryReading(1600, 265, 2000, false),
        };
        var service = new CaptureService(new FakeSource(readings));

        var result = await service.CaptureAsync(runId, CancellationToken.None);

        try
        {
            Assert.Equal(4, result.Samples);
            Assert.Equal(2, result.Faults);
            Assert.Equal("FAIL", result.Verdict);

            await using var verify = new RangeOpsContext();
            var run = await verify.TestRuns.FindAsync(runId);
            Assert.NotNull(run);
            Assert.Equal("FAIL", run!.Status);
            Assert.NotNull(run.StartedAt);
            Assert.NotNull(run.EndedAt);
            Assert.Equal(4, verify.TelemetrySamples.Count(s => s.TestRunId == runId));
        }
        finally
        {
            await using var cleanup = new RangeOpsContext();
            var m = cleanup.Missions.Single(x => x.Name == marker);
            cleanup.Missions.Remove(m); // cascade removes run + samples
            await cleanup.SaveChangesAsync();
        }
    }
}
