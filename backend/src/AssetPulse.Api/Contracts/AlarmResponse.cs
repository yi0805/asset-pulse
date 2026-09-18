using AssetPulse.Api.Domain;

namespace AssetPulse.Api.Contracts;

public sealed record AlarmResponse(
    int Id,
    int AssetId,
    string AssetCode,
    string AssetName,
    AlarmSeverity Severity,
    string Message,
    AlarmStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcknowledgedAt,
    DateTimeOffset? ResolvedAt);
