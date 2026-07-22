using Microsoft.EntityFrameworkCore;
using SemanticLayerManager.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Semantic layer store (our own code-first schema).
builder.Services.AddDbContext<SemanticStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SemanticStore")));

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
