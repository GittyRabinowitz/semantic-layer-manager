namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>Parses an external metadata file and merges it into the semantic store.</summary>
public interface IMetadataImportService
{
    /// <summary>
    /// Reads and validates the JSON metadata stream, merges it, and persists the result.
    /// </summary>
    /// <exception cref="MetadataValidationException">The file is malformed or invalid.</exception>
    Task<MetadataImportReport> ImportAsync(Stream jsonStream, CancellationToken cancellationToken = default);
}
