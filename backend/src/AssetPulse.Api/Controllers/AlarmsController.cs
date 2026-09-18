using AssetPulse.Api.Contracts;
using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using AssetPulse.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Controllers;

[Route("api/alarms")]
public sealed class AlarmsController(AssetPulseDbContext dbContext) : ApiControllerBase
{
    /// <summary>Returns a filtered, newest-first, paginated alarm list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AlarmResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AlarmResponse>>> GetAlarms(
        [FromQuery] AlarmListQuery query,
        CancellationToken cancellationToken)
    {
        if (query.AssetId is <= 0)
        {
            return InvalidFilterProblem("assetId", "Asset ID must be a positive integer.");
        }

        if (!IsDefined(query.Severity))
        {
            return InvalidFilterProblem("severity", "Severity must be Warning or Critical.");
        }

        if (!IsDefined(query.Status))
        {
            return InvalidFilterProblem("status", "Status must be Active, Acknowledged, or Resolved.");
        }

        if (!Pagination.TryCreate(query.Page, query.PageSize, out var pagination, out var errors))
        {
            return ValidationProblemResult(errors);
        }

        IQueryable<Alarm> alarms = dbContext.Alarms.AsNoTracking();
        if (query.AssetId is { } assetId)
        {
            alarms = alarms.Where(alarm => alarm.AssetId == assetId);
        }

        if (query.Severity is { } severity)
        {
            alarms = alarms.Where(alarm => alarm.Severity == severity);
        }

        if (query.Status is { } status)
        {
            alarms = alarms.Where(alarm => alarm.Status == status);
        }

        var totalCount = await alarms.CountAsync(cancellationToken);
        var items = pagination.Skip > int.MaxValue
            ? []
            : await alarms
                .OrderByDescending(alarm => alarm.CreatedAt)
                .ThenByDescending(alarm => alarm.Id)
                .Skip((int)pagination.Skip)
                .Take(pagination.PageSize)
                .Select(alarm => new AlarmResponse(
                    alarm.Id,
                    alarm.AssetId,
                    alarm.Asset!.AssetCode,
                    alarm.Asset.Name,
                    alarm.Severity,
                    alarm.Message,
                    alarm.Status,
                    alarm.CreatedAt,
                    alarm.AcknowledgedAt,
                    alarm.ResolvedAt))
                .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AlarmResponse>(items, pagination.Page, pagination.PageSize, totalCount));
    }
}
