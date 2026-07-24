using System.Text.Json;

namespace SemanticLayerManager.Api.Domain;

/// <summary>
/// A single column of a <see cref="SemanticEntity"/>, enriched with business metadata.
/// Each field is the merge of three sources: the physical schema (introspection),
/// an external metadata file, and manual edits — see <see cref="Source"/>.
/// </summary>
public class SemanticField
{
    public int Id { get; set; }

    public int EntityId { get; set; }
    public SemanticEntity Entity { get; set; } = null!;

    // ── Physical facts (from introspection) ──

    /// <summary>Physical column name (e.g. "eml").</summary>
    public required string PhysicalColumn { get; set; }

    /// <summary>Physical data type as reported by the source DB (e.g. "nvarchar(255)").</summary>
    public string? PhysicalType { get; set; }

    // ── Business enrichment: typed core the engine reasons about ──

    /// <summary>Business-friendly name (e.g. "Email"). Null =&gt; the field is unmapped.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional business description of the field.</summary>
    public string? Description { get; set; }

    /// <summary>Marks personally-identifiable data; drives masking in the consumer UI.</summary>
    public bool IsPii { get; set; }

    /// <summary>Hidden fields are excluded from the consumer UI.</summary>
    public bool Hidden { get; set; }

    /// <summary>Optional business category / grouping.</summary>
    public string? Category { get; set; }

    // ── Business enrichment: flexible, heterogeneous per-column extras ──

    /// <summary>
    /// Free-form JSON bag for column-specific extras the engine does not branch on
    /// (e.g. currency, number format, value labels). Null when there are none.
    /// </summary>
    public string? CustomProperties { get; set; }

    // ── Reconciliation bookkeeping ──

    /// <summary>Status relative to the physical schema (mapped / unmapped / orphaned / type-changed).</summary>
    public MappingStatus Status { get; set; } = MappingStatus.Unmapped;

    /// <summary>Origin of the current values; used by the merge policy.</summary>
    public MetadataSource Source { get; set; } = MetadataSource.Introspection;

    /// <summary>UTC timestamp of the last write; supports last-write-wins.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // ── Derived helpers ──

    /// <summary>
    /// The status a physically-present field should have based on whether it is named:
    /// <see cref="MappingStatus.Mapped"/> once it has a business name, otherwise
    /// <see cref="MappingStatus.Unmapped"/>. Callers decide whether to preserve
    /// <see cref="MappingStatus.Orphaned"/> / <see cref="MappingStatus.TypeChanged"/>.
    /// </summary>
    public MappingStatus PresentStatus =>
        string.IsNullOrWhiteSpace(DisplayName) ? MappingStatus.Unmapped : MappingStatus.Mapped;

    /// <summary>Parses the <see cref="CustomProperties"/> JSON bag, or null when empty.</summary>
    public JsonElement? GetCustomProperties() =>
        string.IsNullOrWhiteSpace(CustomProperties) ? null : JsonSerializer.Deserialize<JsonElement>(CustomProperties);
}
