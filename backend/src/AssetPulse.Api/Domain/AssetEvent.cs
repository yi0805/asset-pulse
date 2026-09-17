namespace AssetPulse.Api.Domain;

public sealed class AssetEvent
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public AssetStatus? PreviousStatus { get; set; }

    public AssetStatus NewStatus { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public required string Description { get; set; }

    public Asset? Asset { get; set; }
}
