using System.Text.Json;
using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Application.Metadata;
using SemanticLayerManager.Api.Application.Sync;
using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Tests;

public class MetadataMergerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddHours(1);

    // ── helpers ──

    private static PhysicalTable Table(string name, params (string col, string type)[] columns) =>
        new(name, columns
            .Select((c, i) => new PhysicalColumn(c.col, c.type, false, i == 0, i + 1))
            .ToList());

    private static List<SemanticEntity> Seed(params PhysicalTable[] tables)
    {
        var entities = new List<SemanticEntity>();
        SchemaReconciler.Reconcile(tables, entities, T0);
        return entities;
    }

    private static SemanticField Field(List<SemanticEntity> entities, string table, string column) =>
        entities.Single(e => e.PhysicalTable == table).Fields.Single(f => f.PhysicalColumn == column);

    private static MetadataFile File(params MetadataEntity[] entities) => new() { Entities = entities.ToList() };

    private static MetadataEntity Ent(string table, string? display, params MetadataField[] fields) =>
        new() { Table = table, DisplayName = display, Fields = fields.ToList() };

    private static MetadataField Fld(
        string column, string? display = null, bool? isPii = null,
        string? category = null, bool? hidden = null, string? customJson = null) =>
        new()
        {
            Column = column,
            DisplayName = display,
            IsPii = isPii,
            Category = category,
            Hidden = hidden,
            CustomProperties = customJson is null ? null : JsonSerializer.Deserialize<JsonElement>(customJson)
        };

    // ── tests ──

    [Fact]
    public void Merge_AppliesEnrichment_AndMapsField()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));

        var file = File(Ent("cust_t", "Customer",
            Fld("eml", "Email", isPii: true, category: "Contact", customJson: "{\"mask\":\"partial\"}")));

        var report = MetadataMerger.Merge(file, entities, T1);

        Assert.Equal(1, report.FieldsApplied);
        var eml = Field(entities, "cust_t", "eml");
        Assert.Equal("Email", eml.DisplayName);
        Assert.True(eml.IsPii);
        Assert.Equal("Contact", eml.Category);
        Assert.Equal(MappingStatus.Mapped, eml.Status);
        Assert.Equal(MetadataSource.File, eml.Source);
        Assert.Contains("mask", eml.CustomProperties);
        Assert.Equal("Customer", entities.Single(e => e.PhysicalTable == "cust_t").DisplayName);
    }

    [Fact]
    public void Merge_SameFileTwice_IsIdempotent()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));
        var file = File(Ent("cust_t", "Customer", Fld("eml", "Email", isPii: true)));

        MetadataMerger.Merge(file, entities, T1);
        var second = MetadataMerger.Merge(file, entities, T1.AddHours(1));

        Assert.Equal(0, second.FieldsApplied);
        Assert.Equal(1, second.FieldsUnchanged);
    }

    [Fact]
    public void Merge_UnmatchedColumn_IsReported_NotInvented()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int")));

        var file = File(Ent("cust_t", null, Fld("does_not_exist", "Ghost")));
        var report = MetadataMerger.Merge(file, entities, T1);

        Assert.Equal(1, report.FieldsUnmatched);
        Assert.Contains("cust_t.does_not_exist", report.UnmatchedColumns);
        Assert.Single(entities[0].Fields); // nothing invented
    }

    [Fact]
    public void Merge_UnknownTable_AllFieldsUnmatched()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int")));

        var file = File(Ent("no_such_table", "X", Fld("a", "A"), Fld("b", "B")));
        var report = MetadataMerger.Merge(file, entities, T1);

        Assert.Equal(2, report.FieldsUnmatched);
        Assert.Single(entities); // no entity created
    }

    [Fact]
    public void Merge_OnlyChangedProperty_CountsAsApplied()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));
        MetadataMerger.Merge(File(Ent("cust_t", null, Fld("eml", "Email", category: "Contact"))), entities, T1);

        // Re-import with only the category changed.
        var report = MetadataMerger.Merge(
            File(Ent("cust_t", null, Fld("eml", "Email", category: "PII-Data"))), entities, T1.AddHours(2));

        Assert.Equal(1, report.FieldsApplied);
        Assert.Equal("PII-Data", Field(entities, "cust_t", "eml").Category);
    }

    [Fact]
    public void Merge_LastWriteWins_FileOverwritesUserValue()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));
        var eml = Field(entities, "cust_t", "eml");
        eml.DisplayName = "Manually Named";
        eml.Source = MetadataSource.User;
        eml.Status = MappingStatus.Mapped;

        MetadataMerger.Merge(File(Ent("cust_t", null, Fld("eml", "Email From File"))), entities, T1);

        // Pure last-write-wins: the file overwrites the earlier manual value.
        Assert.Equal("Email From File", eml.DisplayName);
        Assert.Equal(MetadataSource.File, eml.Source);
    }
}
