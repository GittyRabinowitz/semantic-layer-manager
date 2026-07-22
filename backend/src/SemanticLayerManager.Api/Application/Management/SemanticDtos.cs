using System.Text.Json;
using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Api.Application.Management;

/// <summary>A semantic entity with its fields, for the management UI.</summary>
public record SemanticEntityDto(
    int Id,
    string PhysicalTable,
    string? DisplayName,
    string? Description,
    IReadOnlyList<SemanticFieldDto> Fields);

/// <summary>A semantic field with its physical facts and business enrichment.</summary>
public record SemanticFieldDto(
    int Id,
    string PhysicalColumn,
    string? PhysicalType,
    string? DisplayName,
    string? Description,
    bool IsPii,
    bool Hidden,
    string? Category,
    JsonElement? CustomProperties,
    MappingStatus Status,
    MetadataSource Source,
    DateTime LastModified);

/// <summary>The editable state of a field submitted from the management UI (manual mapping).</summary>
public record UpdateFieldRequest(
    string? DisplayName,
    string? Description,
    bool IsPii,
    bool Hidden,
    string? Category,
    JsonElement? CustomProperties);
