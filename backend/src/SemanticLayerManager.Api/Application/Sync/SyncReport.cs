namespace SemanticLayerManager.Api.Application.Sync;

/// <summary>The kind of change the sync applied to a single field.</summary>
public enum SyncChangeType
{
    /// <summary>A newly discovered physical column (added as unmapped).</summary>
    New,

    /// <summary>A column that no longer exists physically (enrichment preserved).</summary>
    Orphaned,

    /// <summary>A column whose physical data type changed.</summary>
    TypeChanged,

    /// <summary>A previously orphaned/flagged column that is present and stable again.</summary>
    Restored
}

/// <summary>A single field-level change produced by a sync run.</summary>
public record SyncChange(string Table, string Column, SyncChangeType ChangeType, string? Detail);

/// <summary>
/// Summary of a sync run: what changed this run, plus current totals.
/// Surfaced to the management UI so new/removed/changed columns are visible.
/// </summary>
public record SyncReport(
    int TablesTotal,
    int ColumnsTotal,
    int NewColumns,
    int OrphanedColumns,
    int TypeChangedColumns,
    int RestoredColumns,
    int MappedColumns,
    int UnmappedColumns,
    IReadOnlyList<SyncChange> Changes);
