using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Application.Sync;
using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Tests;

public class SchemaReconcilerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── helpers ──

    private static PhysicalTable Table(string name, params (string col, string type)[] columns) =>
        new(name, columns
            .Select((c, i) => new PhysicalColumn(c.col, c.type, IsNullable: false, IsPrimaryKey: i == 0, Ordinal: i + 1))
            .ToList());

    /// <summary>A freshly-reconciled store for the given physical tables.</summary>
    private static List<SemanticEntity> Seed(params PhysicalTable[] tables)
    {
        var entities = new List<SemanticEntity>();
        SchemaReconciler.Reconcile(tables, entities, T0);
        return entities;
    }

    private static SemanticField Field(List<SemanticEntity> entities, string table, string column) =>
        entities.Single(e => e.PhysicalTable == table).Fields.Single(f => f.PhysicalColumn == column);

    // ── tests ──

    [Fact]
    public void FreshStore_CreatesEntitiesAndUnmappedFields()
    {
        var physical = new[] { Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")) };
        var entities = new List<SemanticEntity>();

        var result = SchemaReconciler.Reconcile(physical, entities, T0);

        Assert.Single(entities);
        Assert.Single(result.AddedEntities);
        Assert.Equal(2, entities[0].Fields.Count);
        Assert.All(entities[0].Fields, f => Assert.Equal(MappingStatus.Unmapped, f.Status));
        Assert.All(entities[0].Fields, f => Assert.Equal(MetadataSource.Introspection, f.Source));
        Assert.Equal(2, result.Report.NewColumns);
        Assert.Equal(2, result.Report.UnmappedColumns);
        Assert.Equal("nvarchar(255)", Field(entities, "cust_t", "eml").PhysicalType);
    }

    [Fact]
    public void ResyncWithNoChanges_IsIdempotent()
    {
        var physical = new[] { Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")) };
        var entities = Seed(physical);

        var result = SchemaReconciler.Reconcile(physical, entities, T0.AddMinutes(5));

        Assert.Empty(result.Report.Changes);
        Assert.Empty(result.AddedEntities);
        // No spurious writes: timestamps stay at the original seed time.
        Assert.All(entities.SelectMany(e => e.Fields), f => Assert.Equal(T0, f.LastModified));
    }

    [Fact]
    public void NewColumn_IsAddedAsUnmapped()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int")));

        var withPhone = new[] { Table("cust_t", ("cust_id", "int"), ("phone", "nvarchar(30)")) };
        var result = SchemaReconciler.Reconcile(withPhone, entities, T0.AddDays(1));

        Assert.Equal(1, result.Report.NewColumns);
        var phone = Field(entities, "cust_t", "phone");
        Assert.Equal(MappingStatus.Unmapped, phone.Status);
        Assert.Equal("nvarchar(30)", phone.PhysicalType);
    }

    [Fact]
    public void RemovedColumn_IsOrphaned_AndEnrichmentPreserved()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));
        // Simulate a user having mapped the email column.
        var eml = Field(entities, "cust_t", "eml");
        eml.DisplayName = "Email";
        eml.IsPii = true;
        eml.Status = MappingStatus.Mapped;
        eml.Source = MetadataSource.User;

        var withoutEml = new[] { Table("cust_t", ("cust_id", "int")) };
        var result = SchemaReconciler.Reconcile(withoutEml, entities, T0.AddDays(1));

        Assert.Equal(1, result.Report.OrphanedColumns);
        Assert.Equal(MappingStatus.Orphaned, eml.Status);
        // Enrichment must survive the column disappearing.
        Assert.Equal("Email", eml.DisplayName);
        Assert.True(eml.IsPii);
    }

    [Fact]
    public void TypeChange_IsFlagged_AndPhysicalTypeUpdated()
    {
        var entities = Seed(Table("ord_hdr", ("ord_id", "int"), ("tot_amt", "decimal(12,2)")));

        var changed = new[] { Table("ord_hdr", ("ord_id", "int"), ("tot_amt", "decimal(18,4)")) };
        var result = SchemaReconciler.Reconcile(changed, entities, T0.AddDays(1));

        Assert.Equal(1, result.Report.TypeChangedColumns);
        var amount = Field(entities, "ord_hdr", "tot_amt");
        Assert.Equal(MappingStatus.TypeChanged, amount.Status);
        Assert.Equal("decimal(18,4)", amount.PhysicalType);
        Assert.Contains(result.Report.Changes,
            c => c.ChangeType == SyncChangeType.TypeChanged && c.Detail == "decimal(12,2) -> decimal(18,4)");
    }

    [Fact]
    public void ReappearingColumn_IsRestoredToMappedWhenNamed()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")));
        var eml = Field(entities, "cust_t", "eml");
        eml.DisplayName = "Email";
        eml.Status = MappingStatus.Mapped;

        // Column disappears, then comes back with the same type.
        SchemaReconciler.Reconcile(new[] { Table("cust_t", ("cust_id", "int")) }, entities, T0.AddDays(1));
        Assert.Equal(MappingStatus.Orphaned, eml.Status);

        var result = SchemaReconciler.Reconcile(
            new[] { Table("cust_t", ("cust_id", "int"), ("eml", "nvarchar(255)")) }, entities, T0.AddDays(2));

        Assert.Equal(1, result.Report.RestoredColumns);
        Assert.Equal(MappingStatus.Mapped, eml.Status); // had a business name -> Mapped
    }

    [Fact]
    public void NewTable_IsAddedAsSeparateEntity()
    {
        var entities = Seed(Table("cust_t", ("cust_id", "int")));

        var twoTables = new[]
        {
            Table("cust_t", ("cust_id", "int")),
            Table("prod_t", ("prod_id", "int"), ("prod_nm", "nvarchar(150)"))
        };
        var result = SchemaReconciler.Reconcile(twoTables, entities, T0.AddDays(1));

        Assert.Equal(2, entities.Count);
        Assert.Single(result.AddedEntities);
        Assert.Equal("prod_t", result.AddedEntities[0].PhysicalTable);
        Assert.Equal(2, result.Report.NewColumns);
    }
}
