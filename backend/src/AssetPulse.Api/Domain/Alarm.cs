namespace AssetPulse.Api.Domain;

public sealed class Alarm
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public AlarmSeverity Severity { get; set; }

    public required string Message { get; set; }

    public AlarmStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public Asset? Asset { get; set; }
}
