using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using AssetPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace AssetPulse.Api.Tests;

public sealed class GeneratedSqlInspectionTests(ITestOutputHelper output)
{
    [Fact]
    public void Representative_read_queries_are_inspected_for_server_side_translation()
    {
        using var dbContext = new AssetPulseDbContext(new DbContextOptionsBuilder<AssetPulseDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AssetPulse_SqlInspection")
            .Options);

        var assetList = dbContext.Assets
            .AsNoTracking()
            .Where(asset => asset.Type == "Pump" && asset.Alarms.Any(alarm =>
                alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Warning))
            .OrderBy(asset => asset.AssetCode)
            .ThenBy(asset => asset.Id)
            .Skip(20)
            .Take(20)
            .Select(AssetProjection.Response)
            .ToQueryString();
        var alarms = dbContext.Alarms
            .AsNoTracking()
            .Where(alarm => alarm.Status == AlarmStatus.Active && alarm.Severity == AlarmSeverity.Critical)
            .OrderByDescending(alarm => alarm.CreatedAt)
            .ThenByDescending(alarm => alarm.Id)
            .Skip(20)
            .Take(20)
            .Select(alarm => new { alarm.Id, alarm.AssetId, alarm.Asset!.AssetCode })
            .ToQueryString();
        var events = dbContext.AssetEvents
            .AsNoTracking()
            .Where(assetEvent => assetEvent.AssetId == 1)
            .OrderByDescending(assetEvent => assetEvent.Timestamp)
            .ThenByDescending(assetEvent => assetEvent.Id)
            .Skip(20)
            .Take(20)
            .ToQueryString();

        output.WriteLine($"Asset list SQL:{Environment.NewLine}{assetList}");
        output.WriteLine($"Filtered alarms SQL:{Environment.NewLine}{alarms}");
        output.WriteLine($"Asset events SQL:{Environment.NewLine}{events}");
        Assert.NotEmpty(assetList);
        Assert.NotEmpty(alarms);
        Assert.NotEmpty(events);
    }
}
