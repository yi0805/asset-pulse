using AssetPulse.Api.Domain;

namespace AssetPulse.Api.Contracts;

public sealed record AssetEventResponse(
    int Id,
    int AssetId,
    AssetStatus? PreviousStatus,
    AssetStatus NewStatus,
    DateTimeOffset Timestamp,
    string Description);
