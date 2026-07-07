namespace RangeOps.Core.Models;

/// <summary>One telemetry sample captured from the rig (maps to "telemetry_samples").</summary>
public class TelemetrySample
{
    public long Id { get; set; }
    public int TestRunId { get; set; }
    public DateTime SampleTs { get; set; }
    public float AltitudeFt { get; set; }
    public float AirspeedKt { get; set; }
    public float VerticalSpeedFpm { get; set; }
    public bool LinkDropout { get; set; }

    public TestRun? TestRun { get; set; }
}
