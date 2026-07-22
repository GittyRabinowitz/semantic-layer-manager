using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Infrastructure.Persistence;

namespace SemanticLayerManager.Api.Application.Sync;

/// <summary>
/// Reads the source schema via introspection, reconciles it against the semantic
/// store using <see cref="SchemaReconciler"/>, and persists the changes.
/// The reconcile itself is pure; this class only wires I/O around it.
/// </summary>
public class SyncService(
    SemanticStoreDbContext db,
    ISchemaIntrospector introspector,
    TimeProvider timeProvider) : ISyncService
{
    public async Task<SyncReport> SyncAsync(CancellationToken cancellationToken = default)
    {
        var physicalTables = await introspector.GetSchemaAsync(cancellationToken);

        var entities = await db.Entities
            .Include(e => e.Fields)
            .ToListAsync(cancellationToken);

        var result = SchemaReconciler.Reconcile(physicalTables, entities, timeProvider.GetUtcNow().UtcDateTime);

        if (result.AddedEntities.Count > 0)
            db.Entities.AddRange(result.AddedEntities);

        await db.SaveChangesAsync(cancellationToken);

        return result.Report;
    }
}
