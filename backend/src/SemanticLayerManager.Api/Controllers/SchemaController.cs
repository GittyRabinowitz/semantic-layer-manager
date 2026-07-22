using Microsoft.AspNetCore.Mvc;
using SemanticLayerManager.Api.Application.Introspection;

namespace SemanticLayerManager.Api.Controllers;

/// <summary>
/// Diagnostic endpoint that exposes the raw physical schema of the source
/// database as read by introspection (before any semantic enrichment).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SchemaController(ISchemaIntrospector introspector) : ControllerBase
{
    /// <summary>Returns the source database's tables and columns with their physical types.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PhysicalTable>>> GetSchema(CancellationToken cancellationToken)
        => Ok(await introspector.GetSchemaAsync(cancellationToken));
}
