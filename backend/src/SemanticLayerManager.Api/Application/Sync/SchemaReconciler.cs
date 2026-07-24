using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Api.Application.Sync;

/// <summary>Outcome of a reconcile: the report plus entities that must be inserted.</summary>
public record ReconcileResult(SyncReport Report, IReadOnlyList<SemanticEntity> AddedEntities);

/// <summary>
/// Pure diff/merge engine at the heart of the sync process. Reconciles the physical
/// schema (from introspection) against the existing semantic model:
/// adds new columns as unmapped, flags removed columns as orphaned (without deleting
/// their enrichment), detects type changes, and restores columns that reappear.
///
/// Deliberately free of any DB or I/O dependency so it can be unit-tested directly.
/// It mutates the tracked <paramref name="existingEntities"/> in place and reports
/// brand-new entities separately (the caller inserts those).
/// </summary>
public static class SchemaReconciler
{
    public static ReconcileResult Reconcile(
        IReadOnlyList<PhysicalTable> physicalTables,
        List<SemanticEntity> existingEntities,
        DateTime timestampUtc)
    {
        var changes = new List<SyncChange>();
        var addedEntities = new List<SemanticEntity>();

        var entityByTable = existingEntities.ToDictionary(
            e => e.PhysicalTable, StringComparer.OrdinalIgnoreCase);

        foreach (var table in physicalTables)
        {
            if (!entityByTable.TryGetValue(table.Name, out var entity))
            {
                entity = new SemanticEntity { PhysicalTable = table.Name };
                entityByTable[table.Name] = entity;
                existingEntities.Add(entity);
                addedEntities.Add(entity);
            }

            ReconcileColumns(entity, table, timestampUtc, changes);
        }

        OrphanFieldsOfMissingTables(physicalTables, existingEntities, timestampUtc, changes);

        return new ReconcileResult(BuildReport(existingEntities, changes), addedEntities);
    }

    private static void ReconcileColumns(
        SemanticEntity entity, PhysicalTable table, DateTime timestampUtc, List<SyncChange> changes)
    {
        var fieldByColumn = entity.Fields.ToDictionary(
            f => f.PhysicalColumn, StringComparer.OrdinalIgnoreCase);
        var physicalColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in table.Columns)
        {
            physicalColumns.Add(column.Name);

            if (!fieldByColumn.TryGetValue(column.Name, out var field))
            {
                entity.Fields.Add(new SemanticField
                {
                    PhysicalColumn = column.Name,
                    PhysicalType = column.DataType,
                    Status = MappingStatus.Unmapped,
                    Source = MetadataSource.Introspection,
                    LastModified = timestampUtc
                });
                changes.Add(new SyncChange(table.Name, column.Name, SyncChangeType.New, column.DataType));
                continue;
            }

            if (!string.Equals(field.PhysicalType, column.DataType, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new SyncChange(table.Name, column.Name, SyncChangeType.TypeChanged,
                    $"{field.PhysicalType} -> {column.DataType}"));
                field.PhysicalType = column.DataType;
                field.Status = MappingStatus.TypeChanged;
                field.LastModified = timestampUtc;
            }
            else if (field.Status is MappingStatus.Orphaned or MappingStatus.TypeChanged)
            {
                // The column is back / stable again: recompute its normal status.
                field.Status = field.PresentStatus;
                field.LastModified = timestampUtc;
                changes.Add(new SyncChange(table.Name, column.Name, SyncChangeType.Restored, null));
            }
            else
            {
                // Keep status aligned with mapped-ness, but only write on real change
                // (preserves idempotency: a no-op resync produces no changes).
                var expected = field.PresentStatus;
                if (field.Status != expected)
                {
                    field.Status = expected;
                    field.LastModified = timestampUtc;
                }
            }
        }

        // Columns that vanished from this table become orphaned (enrichment kept).
        foreach (var field in entity.Fields)
        {
            if (!physicalColumns.Contains(field.PhysicalColumn) && field.Status != MappingStatus.Orphaned)
            {
                field.Status = MappingStatus.Orphaned;
                field.LastModified = timestampUtc;
                changes.Add(new SyncChange(entity.PhysicalTable, field.PhysicalColumn, SyncChangeType.Orphaned, null));
            }
        }
    }

    private static void OrphanFieldsOfMissingTables(
        IReadOnlyList<PhysicalTable> physicalTables,
        List<SemanticEntity> existingEntities,
        DateTime timestampUtc,
        List<SyncChange> changes)
    {
        var physicalTableNames = physicalTables
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in existingEntities)
        {
            if (physicalTableNames.Contains(entity.PhysicalTable))
                continue;

            foreach (var field in entity.Fields)
            {
                if (field.Status == MappingStatus.Orphaned)
                    continue;

                field.Status = MappingStatus.Orphaned;
                field.LastModified = timestampUtc;
                changes.Add(new SyncChange(entity.PhysicalTable, field.PhysicalColumn, SyncChangeType.Orphaned, null));
            }
        }
    }

    private static SyncReport BuildReport(List<SemanticEntity> entities, List<SyncChange> changes)
    {
        var fields = entities.SelectMany(e => e.Fields).ToList();

        return new SyncReport(
            TablesTotal: entities.Count,
            ColumnsTotal: fields.Count,
            NewColumns: changes.Count(c => c.ChangeType == SyncChangeType.New),
            OrphanedColumns: changes.Count(c => c.ChangeType == SyncChangeType.Orphaned),
            TypeChangedColumns: changes.Count(c => c.ChangeType == SyncChangeType.TypeChanged),
            RestoredColumns: changes.Count(c => c.ChangeType == SyncChangeType.Restored),
            MappedColumns: fields.Count(f => f.Status == MappingStatus.Mapped),
            UnmappedColumns: fields.Count(f => f.Status == MappingStatus.Unmapped),
            Changes: changes);
    }
}
