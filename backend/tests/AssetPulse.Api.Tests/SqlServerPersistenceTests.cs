using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Tests;

public sealed class SqlServerPersistenceTests
{
    [Fact]
    public void DbContext_configures_required_relationships_indexes_and_precise_measurements()
    {
        var options = new DbContextOptionsBuilder<AssetPulseDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AssetPulse_ModelOnly")
            .Options;
        using var dbContext = new AssetPulseDbContext(options);

        var asset = dbContext.Model.FindEntityType(typeof(Asset))!;
        var alarm = dbContext.Model.FindEntityType(typeof(Alarm))!;
        var assetEvent = dbContext.Model.FindEntityType(typeof(AssetEvent))!;

        Assert.Equal(100, asset.FindProperty(nameof(Asset.Name))!.GetMaxLength());
        Assert.Equal(10, asset.FindProperty(nameof(Asset.Temperature))!.GetPrecision());
        Assert.Equal(2, asset.FindProperty(nameof(Asset.Pressure))!.GetScale());
        Assert.Contains(asset.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Asset.AssetCode));
        Assert.Contains(
            alarm.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(new[] { "AssetId", "Status" }));
        Assert.Contains(
            assetEvent.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(new[] { "AssetId", "Timestamp" }));
        Assert.All(asset.GetReferencingForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Equal("datetimeoffset", asset.FindProperty(nameof(Asset.LastUpdated))!.GetColumnType());
    }

    [Fact]
    public async Task InitialMigration_enforces_schema_and_repeatable_development_seed_data()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("ASSET_PULSE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var databaseName = $"AssetPulse_Task002Tests_{Guid.NewGuid():N}";
        var testConnectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = databaseName
        };
        var serverConnectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = "master"
        };

        try
        {
            await CreateDatabaseAsync(serverConnectionStringBuilder.ConnectionString, databaseName);

            var options = new DbContextOptionsBuilder<AssetPulseDbContext>()
                .UseSqlServer(testConnectionStringBuilder.ConnectionString)
                .Options;

            await using (var dbContext = new AssetPulseDbContext(options))
            {
                await dbContext.Database.MigrateAsync();
                await AssertSchemaAsync(dbContext);
                await AssertConstraintsAsync(dbContext);
            }

            await using (var seededContext = new AssetPulseDbContext(options))
            {
                var seeder = new DevelopmentDataSeeder(seededContext);
                await seeder.SeedAsync();

                Assert.Equal(5, await seededContext.Assets.CountAsync(asset => asset.AssetCode.StartsWith("AP-")));
                Assert.Equal(5, await seededContext.Alarms.CountAsync());
                Assert.Equal(11, await seededContext.AssetEvents.CountAsync());
                Assert.Equal(1, await seededContext.Alarms.CountAsync(alarm => alarm.Status == AlarmStatus.Acknowledged));
                Assert.Equal(2, await seededContext.Alarms.CountAsync(alarm => alarm.Status == AlarmStatus.Resolved));

                var statuses = await seededContext.Assets
                    .Select(asset => new
                    {
                        asset.AssetCode,
                        Status = asset.Alarms.Any(alarm =>
                            alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Critical)
                            ? AssetStatus.Critical
                            : asset.Alarms.Any(alarm =>
                                alarm.Status != AlarmStatus.Resolved && alarm.Severity == AlarmSeverity.Warning)
                                ? AssetStatus.Warning
                                : AssetStatus.Healthy
                    })
                    .ToDictionaryAsync(asset => asset.AssetCode, asset => asset.Status);

                Assert.Equal(AssetStatus.Healthy, statuses["AP-100"]);
                Assert.Equal(AssetStatus.Warning, statuses["AP-200"]);
                Assert.Equal(AssetStatus.Critical, statuses["AP-300"]);

                var editedAsset = await seededContext.Assets.SingleAsync(asset => asset.AssetCode == "AP-100");
                editedAsset.Name = "Edited Boiler 01";
                await seededContext.SaveChangesAsync();

                await seeder.SeedAsync();

                Assert.Equal(5, await seededContext.Assets.CountAsync(asset => asset.AssetCode.StartsWith("AP-")));
                Assert.Equal("Edited Boiler 01", await seededContext.Assets
                    .Where(asset => asset.AssetCode == "AP-100")
                    .Select(asset => asset.Name)
                    .SingleAsync());
            }
        }
        finally
        {
            await DropDatabaseAsync(serverConnectionStringBuilder.ConnectionString, databaseName);
        }
    }

    private static async Task AssertSchemaAsync(AssetPulseDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE name IN (
                'IX_Assets_AssetCode',
                'IX_Alarms_AssetId_Status',
                'IX_Alarms_Status_Severity',
                'IX_AssetEvents_AssetId_Timestamp');
            """;

        var indexCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(4, indexCount);
    }

    private static async Task AssertConstraintsAsync(AssetPulseDbContext dbContext)
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.Assets.Add(new Asset
        {
            Name = "Valid asset",
            AssetCode = "VALID-001",
            Type = "Pump",
            Location = "Test plant",
            LastUpdated = now
        });
        await dbContext.SaveChangesAsync();

        dbContext.Assets.Add(new Asset
        {
            Name = "Duplicate asset",
            AssetCode = "VALID-001",
            Type = "Pump",
            Location = "Test plant",
            LastUpdated = now
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        dbContext.ChangeTracker.Clear();

        dbContext.Assets.Add(new Asset
        {
            Name = "Lowercase code",
            AssetCode = "lowercase-001",
            Type = "Pump",
            Location = "Test plant",
            LastUpdated = now
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        dbContext.ChangeTracker.Clear();

        dbContext.Alarms.Add(new Alarm
        {
            AssetId = int.MaxValue,
            Severity = AlarmSeverity.Warning,
            Message = "Unknown asset",
            Status = AlarmStatus.Active,
            CreatedAt = now
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        dbContext.ChangeTracker.Clear();
    }

    private static async Task CreateDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        if (!databaseName.StartsWith("AssetPulse_Task002Tests_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to drop a database outside the Task 002 test naming convention.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }
}
