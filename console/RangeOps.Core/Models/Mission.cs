namespace RangeOps.Core.Models;

/// <summary>A scheduled flight-test mission (maps to the "missions" table).</summary>
public class Mission
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Aircraft { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string Status { get; set; } = "PLANNED";
    public DateTime CreatedAt { get; set; }

    public List<TestRun> TestRuns { get; set; } = new();

    public override string ToString() => $"{Name} ({Aircraft})";
}
