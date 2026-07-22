namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>Raised when an uploaded metadata file is malformed or fails validation.</summary>
public class MetadataValidationException(string message) : Exception(message);
