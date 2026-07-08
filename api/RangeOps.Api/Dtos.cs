namespace RangeOps.Api;

// Response/request shapes — deliberately separate from the EF Core entities so
// the API surface doesn't leak the persistence model.

public record MissionDto(
    int Id, string Name, string Aircraft, string Status,
    DateTime ScheduledStart, DateTime ScheduledEnd, int RunCount);

public record RunDto(int Id, string Name, string Status, string? Notes);

public record MissionDetailDto(
    int Id, string Name, string Aircraft, string Status, List<RunDto> Runs);

public record TelemetrySummaryDto(
    int RunId, int Samples, int LinkDropouts, float MaxAltitudeFt, float MaxAirspeedKt);

public record CreateMissionDto(
    string Name, string Aircraft, DateTime ScheduledStart, DateTime ScheduledEnd);
