namespace SemanticLayerManager.Api.Application.Introspection;

/// <summary>
/// Reads the structure of the source database dynamically (tables, columns,
/// types) — without any compile-time knowledge of its schema. Provider-specific
/// implementations (e.g. SQL Server) live in Infrastructure.
/// </summary>
public interface ISchemaIntrospector
{
    /// <summary>Returns all base tables and their columns from the source database.</summary>
    Task<IReadOnlyList<PhysicalTable>> GetSchemaAsync(CancellationToken cancellationToken = default);
}
