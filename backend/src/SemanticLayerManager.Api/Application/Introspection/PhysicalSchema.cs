namespace SemanticLayerManager.Api.Application.Introspection;

/// <summary>
/// A physical column as reported by the source database's catalog.
/// This is the raw, technical truth — before any business enrichment.
/// </summary>
public record PhysicalColumn(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    int Ordinal);

/// <summary>A physical table (base table) and its columns.</summary>
public record PhysicalTable(
    string Name,
    IReadOnlyList<PhysicalColumn> Columns);
