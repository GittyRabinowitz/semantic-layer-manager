using Dapper;
using Microsoft.Data.SqlClient;
using SemanticLayerManager.Api.Application.Introspection;

namespace SemanticLayerManager.Api.Infrastructure.Introspection;

/// <summary>
/// Reads the source database schema via <c>INFORMATION_SCHEMA</c> using Dapper.
/// The source schema is unknown at compile time, so we deliberately use raw ADO/Dapper
/// here rather than EF Core (which is reserved for our own semantic store).
/// </summary>
public class SqlServerSchemaIntrospector(string connectionString) : ISchemaIntrospector
{
    private const string ColumnsSql = """
        SELECT
            c.TABLE_NAME                             AS TableName,
            c.COLUMN_NAME                            AS ColumnName,
            c.DATA_TYPE                              AS DataType,
            CAST(c.CHARACTER_MAXIMUM_LENGTH AS int)  AS MaxLength,
            CAST(c.NUMERIC_PRECISION AS int)         AS NumericPrecision,
            CAST(c.NUMERIC_SCALE AS int)             AS NumericScale,
            c.IS_NULLABLE                            AS IsNullable,
            c.ORDINAL_POSITION                       AS Ordinal
        FROM INFORMATION_SCHEMA.COLUMNS c
        INNER JOIN INFORMATION_SCHEMA.TABLES t
            ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
        WHERE t.TABLE_TYPE = 'BASE TABLE' AND t.TABLE_SCHEMA = 'dbo'
        ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;
        """;

    private const string PrimaryKeysSql = """
        SELECT
            ku.TABLE_NAME  AS TableName,
            ku.COLUMN_NAME AS ColumnName
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
            ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
           AND tc.TABLE_SCHEMA   = ku.TABLE_SCHEMA
        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_SCHEMA = 'dbo';
        """;

    private static readonly HashSet<string> LengthTypes =
        new(StringComparer.OrdinalIgnoreCase) { "char", "varchar", "nchar", "nvarchar", "binary", "varbinary" };

    private static readonly HashSet<string> PrecisionTypes =
        new(StringComparer.OrdinalIgnoreCase) { "decimal", "numeric" };

    public async Task<IReadOnlyList<PhysicalTable>> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);

        var keyRows = await connection.QueryAsync<KeyRow>(
            new CommandDefinition(PrimaryKeysSql, cancellationToken: cancellationToken));
        var primaryKeys = keyRows.Select(k => (k.TableName, k.ColumnName)).ToHashSet();

        var columnRows = await connection.QueryAsync<ColumnRow>(
            new CommandDefinition(ColumnsSql, cancellationToken: cancellationToken));

        return columnRows
            .GroupBy(r => r.TableName)
            .Select(group => new PhysicalTable(
                group.Key,
                group
                    .OrderBy(c => c.Ordinal)
                    .Select(c => new PhysicalColumn(
                        c.ColumnName,
                        FormatType(c),
                        IsNullable: string.Equals(c.IsNullable, "YES", StringComparison.OrdinalIgnoreCase),
                        IsPrimaryKey: primaryKeys.Contains((c.TableName, c.ColumnName)),
                        c.Ordinal))
                    .ToList()))
            .OrderBy(t => t.Name)
            .ToList();
    }

    /// <summary>Renders a business-readable type string, e.g. "nvarchar(255)", "decimal(12,2)", "int".</summary>
    private static string FormatType(ColumnRow column)
    {
        var type = column.DataType.ToLowerInvariant();

        if (LengthTypes.Contains(type))
        {
            var length = column.MaxLength == -1 ? "max" : column.MaxLength?.ToString();
            return length is null ? type : $"{type}({length})";
        }

        if (PrecisionTypes.Contains(type) && column.NumericPrecision is not null)
        {
            return $"{type}({column.NumericPrecision},{column.NumericScale ?? 0})";
        }

        return type;
    }

    private sealed record ColumnRow(
        string TableName,
        string ColumnName,
        string DataType,
        int? MaxLength,
        int? NumericPrecision,
        int? NumericScale,
        string IsNullable,
        int Ordinal);

    private sealed record KeyRow(string TableName, string ColumnName);
}
