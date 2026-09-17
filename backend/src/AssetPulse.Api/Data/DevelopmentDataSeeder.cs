using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Data;

public sealed class DevelopmentDataSeeder(AssetPulseDbContext dbContext)
{
    private const string SeedMarkerAssetCode = "AP-100";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Assets.AnyAsync(asset => asset.AssetCode == SeedMarkerAssetCode, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var boiler = CreateAsset("Boiler 01", "AP-100", "Boiler", "North plant", 68.40m, 320.00m, now.AddDays(-10));
        var pump = CreateAsset("Cooling Pump 02", "AP-200", "Pump", "North plant", null, 240.50m, now.AddDays(-1));
        var compressor = CreateAsset("Compressor 01", "AP-300", "Compressor", "South plant", 84.20m, null, now.AddHours(-8));
        var conveyor = CreateAsset("Conveyor 03", "AP-400", "Conveyor", "Warehouse", 29.80m, 101.30m, now.AddDays(-2));
        var tank = CreateAsset("Storage Tank 01", "AP-500", "Tank", "Warehouse", 18.10m, 190.00m, now.AddDays(-1));

        pump.Alarms.Add(new Alarm
        {
            Severity = AlarmSeverity.Warning,
            Message = "Cooling pump vibration requires inspection.",
            Status = AlarmStatus.Active,
            CreatedAt = now.AddDays(-1)
        });
        compressor.Alarms.Add(new Alarm
        {
            Severity = AlarmSeverity.Critical,
            Message = "Compressor safety interlock is unavailable.",
            Status = AlarmStatus.Active,
            CreatedAt = now.AddHours(-8)
        });
        conveyor.Alarms.Add(new Alarm
        {
            Severity = AlarmSeverity.Warning,
            Message = "Conveyor belt tracking needs attention.",
            Status = AlarmStatus.Acknowledged,
            CreatedAt = now.AddDays(-3),
            AcknowledgedAt = now.AddDays(-2)
        });
        conveyor.Alarms.Add(new Alarm
        {
            Severity = AlarmSeverity.Critical,
            Message = "Conveyor emergency stop test was delayed.",
            Status = AlarmStatus.Resolved,
            CreatedAt = now.AddDays(-7),
            ResolvedAt = now.AddDays(-5)
        });
        tank.Alarms.Add(new Alarm
        {
            Severity = AlarmSeverity.Warning,
            Message = "Tank access panel was left open.",
            Status = AlarmStatus.Resolved,
            CreatedAt = now.AddDays(-3),
            ResolvedAt = now.AddDays(-1)
        });

        AddEvent(boiler, null, AssetStatus.Healthy, now.AddDays(-10), "Asset created.");
        AddEvent(pump, null, AssetStatus.Healthy, now.AddDays(-8), "Asset created.");
        AddEvent(pump, AssetStatus.Healthy, AssetStatus.Warning, now.AddDays(-1), "Warning alarm raised.");
        AddEvent(compressor, null, AssetStatus.Healthy, now.AddDays(-6), "Asset created.");
        AddEvent(compressor, AssetStatus.Healthy, AssetStatus.Critical, now.AddHours(-8), "Critical alarm raised.");
        AddEvent(conveyor, null, AssetStatus.Healthy, now.AddDays(-10), "Asset created.");
        AddEvent(conveyor, AssetStatus.Healthy, AssetStatus.Critical, now.AddDays(-7), "Critical alarm raised.");
        AddEvent(conveyor, AssetStatus.Critical, AssetStatus.Warning, now.AddDays(-5), "Critical alarm resolved.");
        AddEvent(tank, null, AssetStatus.Healthy, now.AddDays(-5), "Asset created.");
        AddEvent(tank, AssetStatus.Healthy, AssetStatus.Warning, now.AddDays(-3), "Warning alarm raised.");
        AddEvent(tank, AssetStatus.Warning, AssetStatus.Healthy, now.AddDays(-1), "Warning alarm resolved.");

        dbContext.Assets.AddRange(boiler, pump, compressor, conveyor, tank);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Asset CreateAsset(
        string name,
        string assetCode,
        string type,
        string location,
        decimal? temperature,
        decimal? pressure,
        DateTimeOffset lastUpdated) => new()
        {
            Name = name,
            AssetCode = assetCode,
            Type = type,
            Location = location,
            Temperature = temperature,
            Pressure = pressure,
            LastUpdated = lastUpdated
        };

    private static void AddEvent(
        Asset asset,
        AssetStatus? previousStatus,
        AssetStatus newStatus,
        DateTimeOffset timestamp,
        string description)
    {
        asset.Events.Add(new AssetEvent
        {
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = timestamp,
            Description = description
        });
    }
}
