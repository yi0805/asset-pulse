using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Data;

public sealed class AssetPulseDbContext(DbContextOptions<AssetPulseDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<Alarm> Alarms => Set<Alarm>();

    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetPulseDbContext).Assembly);
    }
}
