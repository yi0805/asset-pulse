using System.Linq.Expressions;
using AssetPulse.Api.Contracts;
using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Services;

public interface IAssetReadService
{
    Task<PagedResponse<AssetResponse>> GetAssetsAsync(
        string? search,
        string? type,
        string? location,
        AssetStatus? status,
        Pagination pagination,
        CancellationToken cancellationToken);

    Task<AssetResponse?> GetAssetAsync(int id, CancellationToken cancellationToken);
}

public sealed class AssetReadService(AssetPulseDbContext dbContext) : IAssetReadService
{
    public async Task<PagedResponse<AssetResponse>> GetAssetsAsync(
        string? search,
        string? type,
        string? location,
        AssetStatus? status,
        Pagination pagination,
        CancellationToken cancellationToken)
    {
        IQueryable<Asset> assets = dbContext.Assets.AsNoTracking();

        if (search is not null)
        {
            assets = assets.Where(asset => asset.Name.Contains(search) || asset.AssetCode.Contains(search));
        }

        if (type is not null)
        {
            assets = assets.Where(asset => asset.Type == type);
        }

        if (location is not null)
        {
            assets = assets.Where(asset => asset.Location == location);
        }

        if (status is { } requestedStatus)
        {
            assets = ApplyStatusFilter(assets, requestedStatus);
        }

        var totalCount = await assets.CountAsync(cancellationToken);
        var items = pagination.Skip > int.MaxValue
            ? []
            : await assets
                .OrderBy(asset => asset.AssetCode)
                .ThenBy(asset => asset.Id)
                .Skip((int)pagination.Skip)
                .Take(pagination.PageSize)
                .Select(AssetProjection.Response)
                .ToListAsync(cancellationToken);

        return new PagedResponse<AssetResponse>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public Task<AssetResponse?> GetAssetAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Assets
            .AsNoTracking()
            .Where(asset => asset.Id == id)
            .Select(AssetProjection.Response)
            .SingleOrDefaultAsync(cancellationToken);

    internal static IQueryable<Asset> ApplyStatusFilter(IQueryable<Asset> assets, AssetStatus status) => status switch
    {
        AssetStatus.Critical => assets.Where(asset => asset.Alarms.Any(alarm =>
            alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Critical)),
        AssetStatus.Warning => assets.Where(asset =>
            !asset.Alarms.Any(alarm => alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Critical)
            && asset.Alarms.Any(alarm => alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Warning)),
        AssetStatus.Healthy => assets.Where(asset =>
            !asset.Alarms.Any(alarm => alarm.Status != AlarmStatus.Resolved &&
                (alarm.Severity == AlarmSeverity.Critical || alarm.Severity == AlarmSeverity.Warning))),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported asset status.")
    };
}

public static class AssetProjection
{
    public static readonly Expression<Func<Asset, AssetResponse>> Response = asset => new AssetResponse(
        asset.Id,
        asset.Name,
        asset.AssetCode,
        asset.Type,
        asset.Location,
        asset.Temperature,
        asset.Pressure,
        asset.LastUpdated,
        asset.Alarms.Any(alarm => alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Critical)
            ? AssetStatus.Critical
            : asset.Alarms.Any(alarm => alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Warning)
                ? AssetStatus.Warning
                : AssetStatus.Healthy);
}
