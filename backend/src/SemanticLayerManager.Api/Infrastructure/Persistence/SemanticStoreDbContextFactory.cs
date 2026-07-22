using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SemanticLayerManager.Api.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the context (for migrations)
/// without booting the full web host. Reads the "SemanticStore" connection string
/// from appsettings.
/// </summary>
public class SemanticStoreDbContextFactory : IDesignTimeDbContextFactory<SemanticStoreDbContext>
{
    public SemanticStoreDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<SemanticStoreDbContext>()
            .UseSqlServer(configuration.GetConnectionString("SemanticStore"))
            .Options;

        return new SemanticStoreDbContext(options);
    }
}
