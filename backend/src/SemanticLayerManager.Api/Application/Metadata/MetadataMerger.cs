using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>
/// Pure merge engine that applies an external metadata file onto the semantic model.
/// Policy: last-write-wins with delta detection — a property is written only when it
/// actually differs (so re-importing the same file is a no-op / idempotent). Any field
/// the file touches is stamped <see cref="MetadataSource.File"/> with a fresh timestamp.
/// Columns/tables not present in the store are reported as unmatched, never invented.
/// </summary>
public static class MetadataMerger
{
    public static MetadataImportReport Merge(
        MetadataFile file, List<SemanticEntity> entities, DateTime timestampUtc)
    {
        var entityByTable = entities.ToDictionary(e => e.PhysicalTable, StringComparer.OrdinalIgnoreCase);

        int fieldsInFile = 0, applied = 0, unchanged = 0, unmatched = 0;
        var unmatchedColumns = new List<string>();

        foreach (var fileEntity in file.Entities)
        {
            if (!entityByTable.TryGetValue(fileEntity.Table, out var entity))
            {
                // The physical table is not (yet) known to the store — treat all its
                // fields as unmatched rather than inventing an entity.
                fieldsInFile += fileEntity.Fields.Count;
                unmatched += fileEntity.Fields.Count;
                unmatchedColumns.AddRange(fileEntity.Fields.Select(f => $"{fileEntity.Table}.{f.Column}"));
                continue;
            }

            ApplyEntityEnrichment(entity, fileEntity);

            var fieldByColumn = entity.Fields.ToDictionary(f => f.PhysicalColumn, StringComparer.OrdinalIgnoreCase);

            foreach (var fileField in fileEntity.Fields)
            {
                fieldsInFile++;

                if (!fieldByColumn.TryGetValue(fileField.Column, out var field))
                {
                    unmatched++;
                    unmatchedColumns.Add($"{fileEntity.Table}.{fileField.Column}");
                    continue;
                }

                if (ApplyFieldEnrichment(field, fileField, timestampUtc))
                    applied++;
                else
                    unchanged++;
            }
        }

        return new MetadataImportReport(
            EntitiesInFile: file.Entities.Count,
            FieldsInFile: fieldsInFile,
            FieldsApplied: applied,
            FieldsUnchanged: unchanged,
            FieldsUnmatched: unmatched,
            UnmatchedColumns: unmatchedColumns);
    }

    private static void ApplyEntityEnrichment(SemanticEntity entity, MetadataEntity fileEntity)
    {
        if (fileEntity.DisplayName is not null && entity.DisplayName != fileEntity.DisplayName)
            entity.DisplayName = fileEntity.DisplayName;

        if (fileEntity.Description is not null && entity.Description != fileEntity.Description)
            entity.Description = fileEntity.Description;
    }

    /// <summary>Applies field enrichment; returns true if anything actually changed.</summary>
    private static bool ApplyFieldEnrichment(SemanticField field, MetadataField fileField, DateTime timestampUtc)
    {
        var changed = false;

        if (fileField.DisplayName is not null && field.DisplayName != fileField.DisplayName)
        {
            field.DisplayName = fileField.DisplayName;
            changed = true;
        }

        if (fileField.Description is not null && field.Description != fileField.Description)
        {
            field.Description = fileField.Description;
            changed = true;
        }

        if (fileField.IsPii is { } isPii && field.IsPii != isPii)
        {
            field.IsPii = isPii;
            changed = true;
        }

        if (fileField.Hidden is { } hidden && field.Hidden != hidden)
        {
            field.Hidden = hidden;
            changed = true;
        }

        if (fileField.Category is not null && field.Category != fileField.Category)
        {
            field.Category = fileField.Category;
            changed = true;
        }

        if (fileField.CustomProperties is { } custom)
        {
            var raw = custom.GetRawText();
            if (field.CustomProperties != raw)
            {
                field.CustomProperties = raw;
                changed = true;
            }
        }

        if (changed)
        {
            field.Source = MetadataSource.File;
            field.LastModified = timestampUtc;

            // A named, physically-present field becomes Mapped. Orphaned/type-changed
            // states are left alone — the file cannot resurrect a missing column.
            if (field.Status is not (MappingStatus.Orphaned or MappingStatus.TypeChanged))
                field.Status = field.PresentStatus;
        }

        return changed;
    }
}
