using Microsoft.EntityFrameworkCore;
using RangeOps.Core.Models;

namespace RangeOps.Core.Data;

/// <summary>
/// EF Core context mapped <b>database-first</b> to the shared RangeOps schema
/// (db/schema.sql). No migrations here -- the SQL file owns the tables, and the
/// Django dashboard maps to the very same tables. This context just reads and
/// writes them.
/// </summary>
public class RangeOpsContext : DbContext
{
    private readonly string _connectionString;

    public RangeOpsContext() : this(DbConfig.ConnectionString) { }

    public RangeOpsContext(string connectionString) => _connectionString = connectionString;

    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<TestRun> TestRuns => Set<TestRun>();
    public DbSet<TelemetrySample> TelemetrySamples => Set<TelemetrySample>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Mission>(e =>
        {
            e.ToTable("missions");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.Name).HasColumnName("name");
            e.Property(m => m.Aircraft).HasColumnName("aircraft");
            e.Property(m => m.ScheduledStart).HasColumnName("scheduled_start");
            e.Property(m => m.ScheduledEnd).HasColumnName("scheduled_end");
            e.Property(m => m.Status).HasColumnName("status");
            e.Property(m => m.CreatedAt).HasColumnName("created_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        });

        b.Entity<TestRun>(e =>
        {
            e.ToTable("test_runs");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.MissionId).HasColumnName("mission_id");
            e.Property(r => r.Name).HasColumnName("name");
            e.Property(r => r.Status).HasColumnName("status");
            e.Property(r => r.StartedAt).HasColumnName("started_at");
            e.Property(r => r.EndedAt).HasColumnName("ended_at");
            e.Property(r => r.Notes).HasColumnName("notes");
            e.HasOne(r => r.Mission).WithMany(m => m.TestRuns).HasForeignKey(r => r.MissionId);
        });

        b.Entity<TelemetrySample>(e =>
        {
            e.ToTable("telemetry_samples");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.TestRunId).HasColumnName("test_run_id");
            e.Property(s => s.SampleTs).HasColumnName("sample_ts")
                .HasDefaultValueSql("now()");
            e.Property(s => s.AltitudeFt).HasColumnName("altitude_ft");
            e.Property(s => s.AirspeedKt).HasColumnName("airspeed_kt");
            e.Property(s => s.VerticalSpeedFpm).HasColumnName("vertical_speed_fpm");
            e.Property(s => s.FaultInjected).HasColumnName("fault_injected");
            e.HasOne(s => s.TestRun).WithMany(r => r.Samples).HasForeignKey(s => s.TestRunId);
        });
    }
}
