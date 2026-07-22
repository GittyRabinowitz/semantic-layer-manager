namespace SemanticLayerManager.Api.Application.Management;

/// <summary>Reads the semantic model and applies manual field edits from the management UI.</summary>
public interface IMappingService
{
    /// <summary>Returns all semantic entities with their fields.</summary>
    Task<IReadOnlyList<SemanticEntityDto>> GetEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a manual edit to a single field (stamped <c>Source = User</c>).
    /// Returns the updated field, or null if no field with that id exists.
    /// </summary>
    Task<SemanticFieldDto?> UpdateFieldAsync(int id, UpdateFieldRequest request, CancellationToken cancellationToken = default);
}
