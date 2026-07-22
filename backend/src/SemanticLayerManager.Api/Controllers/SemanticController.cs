using Microsoft.AspNetCore.Mvc;
using SemanticLayerManager.Api.Application.Management;

namespace SemanticLayerManager.Api.Controllers;

/// <summary>Manages the semantic layer: read entities/fields and apply manual edits.</summary>
[ApiController]
[Route("api/semantic")]
public class SemanticController(IMappingService mappingService) : ControllerBase
{
    /// <summary>Returns all semantic entities with their fields.</summary>
    [HttpGet("entities")]
    public async Task<ActionResult<IReadOnlyList<SemanticEntityDto>>> GetEntities(CancellationToken cancellationToken)
        => Ok(await mappingService.GetEntitiesAsync(cancellationToken));

    /// <summary>Applies a manual edit to a single field.</summary>
    [HttpPut("fields/{id:int}")]
    public async Task<ActionResult<SemanticFieldDto>> UpdateField(
        int id, [FromBody] UpdateFieldRequest request, CancellationToken cancellationToken)
    {
        var updated = await mappingService.UpdateFieldAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
}
