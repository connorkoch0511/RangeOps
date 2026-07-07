namespace RangeOps.Core.Models;

/// <summary>A single test point/run within a mission (maps to "test_runs").</summary>
public class TestRun
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }

    public Mission? Mission { get; set; }
    public List<TelemetrySample> Samples { get; set; } = new();

    public override string ToString() => $"{Name} [{Status}]";
}
