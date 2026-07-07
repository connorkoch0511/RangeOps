using Microsoft.EntityFrameworkCore;
using RangeOps.Core.Data;
using RangeOps.Core.Models;
using Xunit;

namespace RangeOps.Tests;

/// <summary>
/// Integration tests that exercise the EF Core "database-first" mapping against
/// the real shared schema. Requires the docker-compose Postgres to be running
/// (the same instance the Django dashboard uses). Each test cleans up after
/// itself so it can run repeatedly.
/// </summary>
public class RangeOpsContextTests
{
    [Fact]
    public async Task Mission_Run_Sample_RoundTrip_ThroughOrm()
    {
        var marker = $"__test_{Guid.NewGuid():N}";
        await using var db = new RangeOpsContext();

        var mission = new Mission
        {
            Name = marker,
            Aircraft = "T-38C",
            ScheduledStart = DateTime.UtcNow,
            ScheduledEnd = DateTime.UtcNow.AddHours(1),
            Status = "PLANNED",
        };
        db.Missions.Add(mission);
        await db.SaveChangesAsync();

        var run = new TestRun { MissionId = mission.Id, Name = "Climb", Status = "RUNNING" };
        db.TestRuns.Add(run);
        await db.SaveChangesAsync();

        db.TelemetrySamples.Add(new TelemetrySample
        {
            TestRunId = run.Id,
            SampleTs = DateTime.UtcNow,
            AltitudeFt = 12000,
            AirspeedKt = 300,
            VerticalSpeedFpm = 1800,
            LinkDropout = true,
        });
        await db.SaveChangesAsync();

        try
        {
            // Read it back through a fresh context to prove it persisted.
            await using var verify = new RangeOpsContext();
            var loaded = await verify.Missions
                .Include(m => m.TestRuns)
                .ThenInclude(r => r.Samples)
                .SingleAsync(m => m.Name == marker);

            Assert.Equal("T-38C", loaded.Aircraft);
            var loadedRun = Assert.Single(loaded.TestRuns);
            var sample = Assert.Single(loadedRun.Samples);
            Assert.True(sample.LinkDropout);
            Assert.Equal(12000, sample.AltitudeFt);
        }
        finally
        {
            // Cascade delete removes runs + samples too.
            db.Missions.Remove(mission);
            await db.SaveChangesAsync();
        }
    }
}
