using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Application.DataAccess;
using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Domain;
using SemanticLayerManager.Api.Infrastructure.Persistence;

namespace SemanticLayerManager.Api.Infrastructure.DataAccess;

/// <summary>
/// Serves consumer data by building a dynamic query over the source database,
/// exposing only mapped, non-hidden columns under their business names and masking PII.
/// Identifiers are validated against the live introspected schema and bracketed
/// (defence-in-depth); all values are passed as parameters — so no SQL injection.
/// </summary>
public class SqlServerDataQueryService(
    SemanticStoreDbContext db,
    ISchemaIntrospector introspector,
    string connectionString) : IDataQueryService
{
    private const int MaxPageSize = 200;

    public async Task<IReadOnlyList<ConsumableEntity>> GetConsumableEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.Entities
            .Include(e => e.Fields)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .Where(e => e.Fields.Any(IsVisible))
            .Select(e => new ConsumableEntity(e.Id, e.DisplayName ?? e.PhysicalTable, e.PhysicalTable))
            .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<DataPage?> GetDataAsync(int entityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var entity = await db.Entities
            .Include(e => e.Fields)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
        if (entity is null)
            return null;

        // Whitelist the table and columns against the live physical schema.
        var schema = await introspector.GetSchemaAsync(cancellationToken);
        var table = schema.FirstOrDefault(t =>
            string.Equals(t.Name, entity.PhysicalTable, StringComparison.OrdinalIgnoreCase));
        if (table is null)
            return null;

        var physicalColumns = table.Columns
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visible = entity.Fields
            .Where(IsVisible)
            .Where(f => physicalColumns.Contains(f.PhysicalColumn))
            .OrderBy(f => f.Id)
            .ToList();
        if (visible.Count == 0)
            return null;

        var orderColumn = table.Columns.FirstOrDefault(c => c.IsPrimaryKey)?.Name ?? table.Columns[0].Name;

        var columnList = string.Join(", ", visible.Select(f => Bracket(f.PhysicalColumn)));
        var bracketedTable = Bracket(table.Name);
        var dataSql =
            $"SELECT {columnList} FROM {bracketedTable} " +
            $"ORDER BY {Bracket(orderColumn)} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        var countSql = $"SELECT COUNT(*) FROM {bracketedTable}";

        await using var connection = new SqlConnection(connectionString);

        var totalRows = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, cancellationToken: cancellationToken));

        var records = await connection.QueryAsync(new CommandDefinition(
            dataSql,
            new { skip = (page - 1) * pageSize, take = pageSize },
            cancellationToken: cancellationToken));

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (IDictionary<string, object> record in records)
        {
            var row = new Dictionary<string, object?>(visible.Count);
            foreach (var field in visible)
            {
                record.TryGetValue(field.PhysicalColumn, out var value);
                row[field.DisplayName!] = field.IsPii ? PiiMasker.Mask(value) : value;
            }
            rows.Add(row);
        }

        var columns = visible
            .Select(f => new DataColumn(f.DisplayName!, f.IsPii, f.GetCustomProperties()))
            .ToList();
        return new DataPage(entity.DisplayName ?? entity.PhysicalTable, columns, rows, page, pageSize, totalRows);
    }

    private static bool IsVisible(SemanticField field) =>
        field.Status == MappingStatus.Mapped && !field.Hidden && !string.IsNullOrWhiteSpace(field.DisplayName);

    private static string Bracket(string identifier) => "[" + identifier.Replace("]", "]]") + "]";
}
