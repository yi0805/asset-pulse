using AssetPulse.Api.Contracts;
using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Services;

public interface IAssetWriteService
{
    Task<int> CreateAsync(NormalizedAssetUpsert request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, NormalizedAssetUpsert request, CancellationToken cancellationToken);
}

public sealed class AssetWriteService(AssetPulseDbContext dbContext, TimeProvider timeProvider) : IAssetWriteService
{
    public async Task<int> CreateAsync(NormalizedAssetUpsert request, CancellationToken cancellationToken)
    {
        var timestamp = timeProvider.GetUtcNow();
        var asset = new Asset
        {
            Name = request.Name,
            AssetCode = request.AssetCode,
            Type = request.Type,
            Location = request.Location,
            Temperature = request.Temperature,
            Pressure = request.Pressure,
            LastUpdated = timestamp
        };
        asset.Events.Add(new AssetEvent
        {
            PreviousStatus = null,
            NewStatus = AssetStatus.Healthy,
            Timestamp = timestamp,
            Description = "Asset created"
        });

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        return asset.Id;
    }

    public async Task<bool> UpdateAsync(int id, NormalizedAssetUpsert request, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);
        if (asset is null)
        {
            return false;
        }

        if (asset.Name == request.Name
            && asset.AssetCode == request.AssetCode
            && asset.Type == request.Type
            && asset.Location == request.Location
            && asset.Temperature == request.Temperature
            && asset.Pressure == request.Pressure)
        {
            return true;
        }

        asset.Name = request.Name;
        asset.AssetCode = request.AssetCode;
        asset.Type = request.Type;
        asset.Location = request.Location;
        asset.Temperature = request.Temperature;
        asset.Pressure = request.Pressure;
        asset.LastUpdated = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static bool IsAssetCodeDuplicate(DbUpdateException exception)
    {
        var sqlException = exception.GetBaseException() as SqlException;
        return sqlException is not null
            && (sqlException.Number == 2601 || sqlException.Number == 2627)
            && sqlException.Message.Contains("IX_Assets_AssetCode", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record NormalizedAssetUpsert(
    string Name,
    string AssetCode,
    string Type,
    string Location,
    decimal? Temperature,
    decimal? Pressure);
