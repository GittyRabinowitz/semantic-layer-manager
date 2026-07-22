namespace SemanticLayerManager.Api.Domain;

/// <summary>
/// Reconciliation status of a semantic field relative to the physical schema.
/// </summary>
public enum MappingStatus
{
    /// <summary>Column exists physically and has been given a business name.</summary>
    Mapped,

    /// <summary>Column exists physically but has not been mapped (no business name yet).</summary>
    Unmapped,

    /// <summary>Enrichment exists but the physical column no longer exists in the source DB.</summary>
    Orphaned,

    /// <summary>Column exists physically but its data type changed since the last sync.</summary>
    TypeChanged
}

/// <summary>
/// Origin of the most recent value written to a semantic field.
/// Drives the merge / last-write-wins bookkeeping.
/// </summary>
public enum MetadataSource
{
    /// <summary>Discovered by schema introspection of the source database.</summary>
    Introspection,

    /// <summary>Provided by an imported external metadata file.</summary>
    File,

    /// <summary>Set manually by a user through the management UI.</summary>
    User
}
