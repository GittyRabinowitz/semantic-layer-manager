namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>Summary of a metadata-file import: what was applied, unchanged, or unmatched.</summary>
public record MetadataImportReport(
    int EntitiesInFile,
    int FieldsInFile,
    int FieldsApplied,
    int FieldsUnchanged,
    int FieldsUnmatched,
    IReadOnlyList<string> UnmatchedColumns);
