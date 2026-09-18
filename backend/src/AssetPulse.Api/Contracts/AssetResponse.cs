using AssetPulse.Api.Domain;

namespace AssetPulse.Api.Contracts;

public sealed record AssetResponse(
    int Id,
    string Name,
    string AssetCode,
    string Type,
    string Location,
    decimal? Temperature,
    decimal? Pressure,
    DateTimeOffset LastUpdated,
    AssetStatus Status);
