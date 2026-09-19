using AssetPulse.Api.Contracts;
using AssetPulse.Api.Domain;
using AssetPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AssetPulse.Api.Controllers;

[Route("api/assets")]
public sealed class AssetsController(
    IAssetReadService assetReadService,
    IAssetWriteService assetWriteService) : ApiControllerBase
{
    /// <summary>Returns a filtered, ordered, paginated asset list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AssetResponse>>> GetAssets(
        [FromQuery] AssetListQuery query,
        CancellationToken cancellationToken)
    {
        if (!IsDefined(query.Status))
        {
            return InvalidFilterProblem("status", "Status must be Healthy, Warning, or Critical.");
        }

        if (!Pagination.TryCreate(query.Page, query.PageSize, out var pagination, out var errors))
        {
            return ValidationProblemResult(errors);
        }

        return Ok(await assetReadService.GetAssetsAsync(
            Normalize(query.Search),
            Normalize(query.Type),
            Normalize(query.Location),
            query.Status,
            pagination,
            cancellationToken));
    }

    /// <summary>Returns one asset with its derived operational status.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponse>> GetAsset(int id, CancellationToken cancellationToken)
    {
        var asset = await assetReadService.GetAssetAsync(id, cancellationToken);
        return asset is null ? NotFoundProblem("The requested asset does not exist.") : Ok(asset);
    }

    /// <summary>Creates an asset and its initial Healthy status history event.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> CreateAsset(
        [FromBody] AssetUpsertRequest? request,
        CancellationToken cancellationToken)
    {
        if (!AssetUpsertValidator.TryNormalize(request, out var normalized, out var errors))
        {
            return AssetValidationProblem(errors);
        }

        try
        {
            var id = await assetWriteService.CreateAsync(normalized!, cancellationToken);
            var response = await assetReadService.GetAssetAsync(id, cancellationToken);
            return Created($"/api/assets/{id}", response);
        }
        catch (DbUpdateException exception) when (AssetWriteService.IsAssetCodeDuplicate(exception))
        {
            return AssetCodeConflictProblem();
        }
    }

    /// <summary>Fully updates the editable fields of an existing asset.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> UpdateAsset(
        int id,
        [FromBody] AssetUpsertRequest? request,
        CancellationToken cancellationToken)
    {
        if (!AssetUpsertValidator.TryNormalize(request, out var normalized, out var errors))
        {
            return AssetValidationProblem(errors);
        }

        try
        {
            if (!await assetWriteService.UpdateAsync(id, normalized!, cancellationToken))
            {
                return NotFoundProblem("The requested asset does not exist.");
            }

            var response = await assetReadService.GetAssetAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (DbUpdateException exception) when (AssetWriteService.IsAssetCodeDuplicate(exception))
        {
            return AssetCodeConflictProblem();
        }
    }
}
