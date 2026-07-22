using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Domain;

namespace SemanticLayerManager.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the semantic layer store — our own, code-first schema that
/// holds the managed mapping. This is intentionally separate from the source
/// database, whose (unknown) schema is read dynamically via introspection.
/// </summary>
public class SemanticStoreDbContext(DbContextOptions<SemanticStoreDbContext> options)
    : DbContext(options)
{
    public DbSet<SemanticEntity> Entities => Set<SemanticEntity>();
    public DbSet<SemanticField> Fields => Set<SemanticField>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SemanticEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PhysicalTable).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(1024);

            // One semantic entity per physical table.
            entity.HasIndex(x => x.PhysicalTable).IsUnique();

            entity.HasMany(x => x.Fields)
                  .WithOne(f => f.Entity)
                  .HasForeignKey(f => f.EntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SemanticField>(field =>
        {
            field.HasKey(x => x.Id);
            field.Property(x => x.PhysicalColumn).HasMaxLength(256).IsRequired();
            field.Property(x => x.PhysicalType).HasMaxLength(128);
            field.Property(x => x.DisplayName).HasMaxLength(256);
            field.Property(x => x.Description).HasMaxLength(1024);
            field.Property(x => x.Category).HasMaxLength(128);

            // Store enums as readable strings rather than opaque ints.
            field.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            field.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);

            // One semantic field per (entity, physical column).
            field.HasIndex(x => new { x.EntityId, x.PhysicalColumn }).IsUnique();
        });
    }
}
