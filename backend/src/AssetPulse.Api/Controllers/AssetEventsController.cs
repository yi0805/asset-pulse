using AssetPulse.Api.Contracts;
using AssetPulse.Api.Data;
using AssetPulse.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Controllers;

[Route("api/assets/{assetId:int}/events")]
public sealed class AssetEventsController(AssetPulseDbContext dbContext) : ApiControllerBase
{
    /// <summary>Returns status history for an existing asset, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AssetEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<AssetEventResponse>>> GetEvents(
        int assetId,
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken)
    {
        if (!Pagination.TryCreate(query.Page, query.PageSize, out var pagination, out var errors))
        {
            return ValidationProblemResult(errors);
        }

        if (!await dbContext.Assets.AsNoTracking().AnyAsync(asset => asset.Id == assetId, cancellationToken))
        {
            return NotFoundProblem("The requested asset does not exist.");
        }

        var events = dbContext.AssetEvents.AsNoTracking().Where(assetEvent => assetEvent.AssetId == assetId);
        var totalCount = await events.CountAsync(cancellationToken);
        var items = pagination.Skip > int.MaxValue
            ? []
            : await events
                .OrderByDescending(assetEvent => assetEvent.Timestamp)
                .ThenByDescending(assetEvent => assetEvent.Id)
                .Skip((int)pagination.Skip)
                .Take(pagination.PageSize)
                .Select(assetEvent => new AssetEventResponse(
                    assetEvent.Id,
                    assetEvent.AssetId,
                    assetEvent.PreviousStatus,
                    assetEvent.NewStatus,
                    assetEvent.Timestamp,
                    assetEvent.Description))
                .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AssetEventResponse>(items, pagination.Page, pagination.PageSize, totalCount));
    }
}
