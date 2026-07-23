using System.Text.Json;

namespace SemanticLayerManager.Api.Application.DataAccess;

/// <summary>An entity that can be browsed through the semantic layer (has visible fields).</summary>
public record ConsumableEntity(int Id, string DisplayName, string PhysicalTable);

/// <summary>
/// A business-named column exposed to the consumer. <see cref="Format"/> carries the
/// column's custom properties (e.g. valueLabels, currency, decimals, date format) so the
/// consumer can present values in business terms.
/// </summary>
public record DataColumn(string Name, bool IsPii, JsonElement? Format);

/// <summary>A page of data returned through the semantic layer (business names, masked PII).</summary>
public record DataPage(
    string Entity,
    IReadOnlyList<DataColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    int Page,
    int PageSize,
    int TotalRows);
