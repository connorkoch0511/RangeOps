using Microsoft.EntityFrameworkCore;
using RangeOps.Api;
using RangeOps.Core.Data;
using RangeOps.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Reuse the shared Core DbContext + connection resolution (env → default → local).
builder.Services.AddDbContext<RangeOpsContext>(o => o.UseNpgsql(DbConfig.ConnectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// --- Missions -------------------------------------------------------------
app.MapGet("/api/missions", async (RangeOpsContext db) =>
    await db.Missions
        .OrderByDescending(m => m.ScheduledStart)
        .Select(m => new MissionDto(
            m.Id, m.Name, m.Aircraft, m.Status,
            m.ScheduledStart, m.ScheduledEnd, m.TestRuns.Count))
        .ToListAsync());

app.MapGet("/api/missions/{id:int}", async (int id, RangeOpsContext db) =>
{
    var m = await db.Missions
        .Include(x => x.TestRuns)
        .FirstOrDefaultAsync(x => x.Id == id);
    return m is null
        ? Results.NotFound()
        : Results.Ok(new MissionDetailDto(
            m.Id, m.Name, m.Aircraft, m.Status,
            m.TestRuns
                .OrderBy(r => r.Id)
                .Select(r => new RunDto(r.Id, r.Name, r.Status, r.Notes))
                .ToList()));
});

app.MapPost("/api/missions", async (CreateMissionDto dto, RangeOpsContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Aircraft))
        return Results.BadRequest(new { error = "name and aircraft are required" });
    if (dto.ScheduledEnd <= dto.ScheduledStart)
        return Results.BadRequest(new { error = "scheduledEnd must be after scheduledStart" });

    var m = new Mission
    {
        Name = dto.Name.Trim(),
        Aircraft = dto.Aircraft.Trim(),
        ScheduledStart = dto.ScheduledStart,
        ScheduledEnd = dto.ScheduledEnd,
        Status = "PLANNED",
    };
    db.Missions.Add(m);
    await db.SaveChangesAsync();
    return Results.Created($"/api/missions/{m.Id}",
        new MissionDto(m.Id, m.Name, m.Aircraft, m.Status, m.ScheduledStart, m.ScheduledEnd, 0));
});

// --- Telemetry ------------------------------------------------------------
app.MapGet("/api/runs/{id:int}/telemetry", async (int id, RangeOpsContext db) =>
{
    if (!await db.TestRuns.AnyAsync(r => r.Id == id))
        return Results.NotFound();

    var q = db.TelemetrySamples.Where(s => s.TestRunId == id);
    var summary = new TelemetrySummaryDto(
        id,
        await q.CountAsync(),
        await q.CountAsync(s => s.LinkDropout),
        await q.Select(s => (float?)s.AltitudeFt).MaxAsync() ?? 0f,
        await q.Select(s => (float?)s.AirspeedKt).MaxAsync() ?? 0f);
    return Results.Ok(summary);
});

app.Run();

// Exposed for WebApplicationFactory in the test project.
public partial class Program { }
