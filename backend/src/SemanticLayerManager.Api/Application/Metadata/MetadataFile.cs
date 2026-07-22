using System.Text.Json;

namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>Root of an external metadata file (business enrichment for the schema).</summary>
public class MetadataFile
{
    public List<MetadataEntity> Entities { get; set; } = [];
}

/// <summary>Enrichment for one physical table.</summary>
public class MetadataEntity
{
    public string Table { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<MetadataField> Fields { get; set; } = [];
}

/// <summary>
/// Enrichment for one physical column. All enrichment properties are optional;
/// an absent property means "leave the current value unchanged".
/// </summary>
public class MetadataField
{
    public string Column { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsPii { get; set; }
    public bool? Hidden { get; set; }
    public string? Category { get; set; }

    /// <summary>Free-form per-column extras (currency, format, value labels, ...).</summary>
    public JsonElement? CustomProperties { get; set; }
}
