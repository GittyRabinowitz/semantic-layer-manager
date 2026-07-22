using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Infrastructure.Persistence;

namespace SemanticLayerManager.Api.Application.Metadata;

/// <summary>
/// Reads a metadata JSON stream, validates it, merges it into the store via
/// <see cref="MetadataMerger"/>, and persists the result.
/// </summary>
public class MetadataImportService(
    SemanticStoreDbContext db,
    TimeProvider timeProvider) : IMetadataImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<MetadataImportReport> ImportAsync(Stream jsonStream, CancellationToken cancellationToken = default)
    {
        MetadataFile? file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<MetadataFile>(jsonStream, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new MetadataValidationException($"Invalid metadata JSON: {ex.Message}");
        }

        if (file is null)
            throw new MetadataValidationException("The metadata file is empty.");

        Validate(file);

        var entities = await db.Entities
            .Include(e => e.Fields)
            .ToListAsync(cancellationToken);

        var report = MetadataMerger.Merge(file, entities, timeProvider.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(cancellationToken);

        return report;
    }

    private static void Validate(MetadataFile file)
    {
        foreach (var entity in file.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Table))
                throw new MetadataValidationException("Every entity must have a non-empty 'table'.");

            var duplicateColumn = entity.Fields
                .GroupBy(f => f.Column, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateColumn is not null)
                throw new MetadataValidationException(
                    $"Entity '{entity.Table}' lists column '{duplicateColumn.Key}' more than once.");

            foreach (var field in entity.Fields)
                if (string.IsNullOrWhiteSpace(field.Column))
                    throw new MetadataValidationException(
                        $"Entity '{entity.Table}' has a field with no 'column'.");
        }
    }
}
