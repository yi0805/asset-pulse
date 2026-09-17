namespace AssetPulse.Api.Domain;

public sealed class Asset
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string AssetCode { get; set; }

    public required string Type { get; set; }

    public required string Location { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Pressure { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    public ICollection<Alarm> Alarms { get; } = new List<Alarm>();

    public ICollection<AssetEvent> Events { get; } = new List<AssetEvent>();
}
