namespace SemanticLayerManager.Api.Application.Sync;

/// <summary>
/// Orchestrates a sync run: reads the source schema, reconciles it against the
/// stored semantic model, persists the result, and returns a report.
/// </summary>
public interface ISyncService
{
    Task<SyncReport> SyncAsync(CancellationToken cancellationToken = default);
}
