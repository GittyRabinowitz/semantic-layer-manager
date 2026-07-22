using Microsoft.AspNetCore.Mvc;
using SemanticLayerManager.Api.Application.Sync;

namespace SemanticLayerManager.Api.Controllers;

/// <summary>Triggers reconciliation of the source schema into the semantic layer.</summary>
[ApiController]
[Route("api/[controller]")]
public class SyncController(ISyncService syncService) : ControllerBase
{
    /// <summary>Runs a sync and returns a report of what changed.</summary>
    [HttpPost]
    public async Task<ActionResult<SyncReport>> Sync(CancellationToken cancellationToken)
        => Ok(await syncService.SyncAsync(cancellationToken));
}
