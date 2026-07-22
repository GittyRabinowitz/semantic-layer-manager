using Microsoft.AspNetCore.Mvc;
using SemanticLayerManager.Api.Application.Metadata;

namespace SemanticLayerManager.Api.Controllers;

/// <summary>Imports an external metadata file into the semantic layer.</summary>
[ApiController]
[Route("api/[controller]")]
public class MetadataController(IMetadataImportService importService) : ControllerBase
{
    /// <summary>Uploads a JSON metadata file and merges it into the store.</summary>
    [HttpPost("import")]
    public async Task<ActionResult<MetadataImportReport>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file was uploaded.");

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await importService.ImportAsync(stream, cancellationToken));
        }
        catch (MetadataValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
