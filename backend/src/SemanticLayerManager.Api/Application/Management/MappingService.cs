using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Domain;
using SemanticLayerManager.Api.Infrastructure.Persistence;

namespace SemanticLayerManager.Api.Application.Management;

/// <summary>
/// Reads the semantic model and applies manual field edits. A manual edit is the
/// interactive counterpart to bulk sync/import: it stamps <see cref="MetadataSource.User"/>.
/// </summary>
public class MappingService(
    SemanticStoreDbContext db,
    TimeProvider timeProvider) : IMappingService
{
    public async Task<IReadOnlyList<SemanticEntityDto>> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.Entities
            .Include(e => e.Fields)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(e => e.DisplayName ?? e.PhysicalTable, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto)
            .ToList();
    }

    public async Task<SemanticFieldDto?> UpdateFieldAsync(
        int id, UpdateFieldRequest request, CancellationToken cancellationToken = default)
    {
        var field = await db.Fields.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (field is null)
            return null;

        field.DisplayName = Normalize(request.DisplayName);
        field.Description = Normalize(request.Description);
        field.IsPii = request.IsPii;
        field.Hidden = request.Hidden;
        field.Category = Normalize(request.Category);
        field.CustomProperties = request.CustomProperties?.GetRawText();

        field.Source = MetadataSource.User;
        field.LastModified = timeProvider.GetUtcNow().UtcDateTime;

        // A manual edit acknowledges any pending flag except a truly missing column.
        if (field.Status != MappingStatus.Orphaned)
            field.Status = string.IsNullOrWhiteSpace(field.DisplayName)
                ? MappingStatus.Unmapped
                : MappingStatus.Mapped;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(field);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SemanticEntityDto ToDto(SemanticEntity entity) => new(
        entity.Id,
        entity.PhysicalTable,
        entity.DisplayName,
        entity.Description,
        entity.Fields
            .OrderBy(f => f.Id)
            .Select(ToDto)
            .ToList());

    private static SemanticFieldDto ToDto(SemanticField field) => new(
        field.Id,
        field.PhysicalColumn,
        field.PhysicalType,
        field.DisplayName,
        field.Description,
        field.IsPii,
        field.Hidden,
        field.Category,
        ParseCustomProperties(field.CustomProperties),
        field.Status,
        field.Source,
        field.LastModified);

    private static JsonElement? ParseCustomProperties(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<JsonElement>(raw);
}
