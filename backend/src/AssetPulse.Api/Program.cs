using AssetPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AssetPulseDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AssetPulse")
        ?? "Server=127.0.0.1,1433;Database=AssetPulse;User Id=sa;TrustServerCertificate=True"));
builder.Services.AddScoped<DevelopmentDataSeeder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapHealthChecks("/api/health");
app.MapControllers();

if (args.Contains("--seed-development-data", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Development seed data can only be applied in the Development environment.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AssetPulseDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();

    await dbContext.Database.MigrateAsync();
    await seeder.SeedAsync();
    return;
}

app.Run();

public partial class Program
{
}
