using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RangeOps.Api;
using RangeOps.Core.Data;
using RangeOps.Core.Models;
using Xunit;

namespace RangeOps.Api.Tests;

/// <summary>
/// Integration tests that boot the API in-memory (WebApplicationFactory) and
/// exercise it over HTTP against the shared Postgres database. Requires the
/// docker-compose Postgres to be running (same as the other integration tests).
/// </summary>
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var resp = await _client.GetAsync("/health");
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateThenGetMission_RoundTrips()
    {
        var marker = $"__api_{Guid.NewGuid():N}";
        var create = new CreateMissionDto(marker, "F-16C",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        var post = await _client.PostAsJsonAsync("/api/missions", create);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<MissionDto>();
        Assert.NotNull(created);
        Assert.Equal(marker, created!.Name);

        try
        {
            var detail = await _client.GetFromJsonAsync<MissionDetailDto>($"/api/missions/{created.Id}");
            Assert.NotNull(detail);
            Assert.Equal("F-16C", detail!.Aircraft);
            Assert.Equal("PLANNED", detail.Status);

            // it also shows up in the list
            var list = await _client.GetFromJsonAsync<List<MissionDto>>("/api/missions");
            Assert.Contains(list!, m => m.Id == created.Id);
        }
        finally
        {
            await using var db = new RangeOpsContext();
            var m = await db.Missions.FindAsync(created.Id);
            if (m is not null) { db.Missions.Remove(m); await db.SaveChangesAsync(); }
        }
    }

    [Fact]
    public async Task CreateMission_EndBeforeStart_ReturnsBadRequest()
    {
        var bad = new CreateMissionDto("x", "y",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(-1));
        var resp = await _client.PostAsJsonAsync("/api/missions", bad);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task TelemetrySummary_CountsSamplesAndDropouts()
    {
        var marker = $"__api_{Guid.NewGuid():N}";
        int runId;
        await using (var db = new RangeOpsContext())
        {
            var m = new Mission
            {
                Name = marker, Aircraft = "T-38C",
                ScheduledStart = DateTime.UtcNow, ScheduledEnd = DateTime.UtcNow.AddHours(1),
                Status = "ACTIVE",
            };
            db.Missions.Add(m);
            await db.SaveChangesAsync();
            var run = new TestRun { MissionId = m.Id, Name = "Climb", Status = "RUNNING" };
            db.TestRuns.Add(run);
            await db.SaveChangesAsync();
            runId = run.Id;
            for (int i = 0; i < 10; i++)
                db.TelemetrySamples.Add(new TelemetrySample
                {
                    TestRunId = runId, SampleTs = DateTime.UtcNow.AddSeconds(i),
                    AltitudeFt = 1000 + i * 100, AirspeedKt = 250 + i,
                    VerticalSpeedFpm = 2000, LinkDropout = i is 4 or 5 or 6,
                });
            await db.SaveChangesAsync();
        }

        try
        {
            var summary = await _client.GetFromJsonAsync<TelemetrySummaryDto>($"/api/runs/{runId}/telemetry");
            Assert.NotNull(summary);
            Assert.Equal(10, summary!.Samples);
            Assert.Equal(3, summary.LinkDropouts);
            Assert.Equal(1900f, summary.MaxAltitudeFt);
        }
        finally
        {
            await using var db = new RangeOpsContext();
            var m = db.Missions.Single(x => x.Name == marker);
            db.Missions.Remove(m);
            await db.SaveChangesAsync();
        }
    }
}
