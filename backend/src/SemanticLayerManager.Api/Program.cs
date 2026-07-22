using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Application.Metadata;
using SemanticLayerManager.Api.Application.Sync;
using SemanticLayerManager.Api.Infrastructure.Introspection;
using SemanticLayerManager.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

// Semantic layer store (our own code-first schema).
builder.Services.AddDbContext<SemanticStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SemanticStore")));

// Source database introspection (dynamic, unknown schema — raw Dapper, not EF).
builder.Services.AddScoped<ISchemaIntrospector>(_ =>
    new SqlServerSchemaIntrospector(
        builder.Configuration.GetConnectionString("SourceDb")
        ?? throw new InvalidOperationException("Missing 'SourceDb' connection string.")));

// Sync engine.
builder.Services.AddScoped<ISyncService, SyncService>();

// Metadata file import.
builder.Services.AddScoped<IMetadataImportService, MetadataImportService>();

var app = builder.Build();

// ── HTTP pipeline ──
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
