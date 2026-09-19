namespace AssetPulse.Api.Contracts;

public sealed record AssetUpsertRequest
{
    public string? Name { get; init; }

    public string? AssetCode { get; init; }

    public string? Type { get; init; }

    public string? Location { get; init; }

    public decimal? Temperature { get; init; }

    public decimal? Pressure { get; init; }
}
