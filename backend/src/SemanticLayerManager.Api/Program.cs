using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using SemanticLayerManager.Api.Application.DataAccess;
using SemanticLayerManager.Api.Application.Introspection;
using SemanticLayerManager.Api.Application.Management;
using SemanticLayerManager.Api.Application.Metadata;
using SemanticLayerManager.Api.Application.Sync;
using SemanticLayerManager.Api.Infrastructure.DataAccess;
using SemanticLayerManager.Api.Infrastructure.Introspection;
using SemanticLayerManager.Api.Infrastructure.Persistence;

const string FrontendCors = "frontend";

var builder = WebApplication.CreateBuilder(args);

// Structured logging.
builder.Services.AddSerilog(config => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console());

// ── Services ──
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

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

// Semantic-layer management (read + manual edits).
builder.Services.AddScoped<IMappingService, MappingService>();

// Consumer data access through the semantic layer (dynamic query over the source DB).
builder.Services.AddScoped<IDataQueryService>(sp => new SqlServerDataQueryService(
    sp.GetRequiredService<SemanticStoreDbContext>(),
    sp.GetRequiredService<ISchemaIntrospector>(),
    builder.Configuration.GetConnectionString("SourceDb")
    ?? throw new InvalidOperationException("Missing 'SourceDb' connection string.")));

var app = builder.Build();

// ── HTTP pipeline ──
app.UseExceptionHandler();          // unhandled exceptions -> RFC 7807 ProblemDetails
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // OpenAPI document at /openapi/v1.json
    app.MapScalarApiReference();    // interactive API reference at /scalar/v1
}

app.UseHttpsRedirection();
app.UseCors(FrontendCors);
app.UseAuthorization();
app.MapControllers();

app.Run();
