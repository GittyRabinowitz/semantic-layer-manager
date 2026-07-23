using Microsoft.AspNetCore.Mvc;
using SemanticLayerManager.Api.Application.DataAccess;

namespace SemanticLayerManager.Api.Controllers;

/// <summary>Consumer-facing data access through the semantic layer.</summary>
[ApiController]
[Route("api/data")]
public class DataController(IDataQueryService dataQueryService) : ControllerBase
{
    /// <summary>Lists entities that expose at least one visible column.</summary>
    [HttpGet("entities")]
    public async Task<ActionResult<IReadOnlyList<ConsumableEntity>>> GetEntities(CancellationToken cancellationToken)
        => Ok(await dataQueryService.GetConsumableEntitiesAsync(cancellationToken));

    /// <summary>Returns a page of data for one entity, through the semantic layer.</summary>
    [HttpGet("{entityId:int}")]
    public async Task<ActionResult<DataPage>> GetData(
        int entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await dataQueryService.GetDataAsync(entityId, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
