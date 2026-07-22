namespace SemanticLayerManager.Api.Domain;

/// <summary>
/// A business entity in the semantic layer, mapped to exactly one physical table
/// in the source database.
/// </summary>
public class SemanticEntity
{
    public int Id { get; set; }

    /// <summary>Physical table name in the source database (e.g. "cust_t").</summary>
    public required string PhysicalTable { get; set; }

    /// <summary>Business-friendly name (e.g. "Customer"). Null until mapped.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional business description of the entity.</summary>
    public string? Description { get; set; }

    /// <summary>The columns of this entity, enriched with business metadata.</summary>
    public ICollection<SemanticField> Fields { get; set; } = new List<SemanticField>();
}
