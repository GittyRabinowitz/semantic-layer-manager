namespace SemanticLayerManager.Api.Application.DataAccess;

/// <summary>
/// Serves data to the consumer through the semantic layer: only mapped, non-hidden
/// columns, presented with business names and with PII masked.
/// </summary>
public interface IDataQueryService
{
    /// <summary>Entities that expose at least one visible column.</summary>
    Task<IReadOnlyList<ConsumableEntity>> GetConsumableEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A page of rows for one entity, through the semantic layer.
    /// Returns null if the entity does not exist or exposes no visible columns.
    /// </summary>
    Task<DataPage?> GetDataAsync(int entityId, int page, int pageSize, CancellationToken cancellationToken = default);
}
