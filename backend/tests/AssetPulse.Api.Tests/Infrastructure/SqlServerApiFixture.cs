using AssetPulse.Api.Data;
using AssetPulse.Api.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AssetPulse.Api.Tests.Infrastructure;

public sealed class SqlServerApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabasePrefix = "AssetPulse_Task003Tests_";
    private readonly string? configuredConnectionString = Environment.GetEnvironmentVariable("ASSET_PULSE_TEST_CONNECTION");
    private string? databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuredConnectionString);

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        databaseName = $"{TestDatabasePrefix}{Guid.NewGuid():N}";
        var connectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = databaseName
        };
        ConnectionString = connectionStringBuilder.ConnectionString;

        var serverConnectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = "master"
        };

        await CreateDatabaseAsync(serverConnectionStringBuilder.ConnectionString, databaseName);
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeDatabaseAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await DisposeDatabaseAsync();
        await base.DisposeAsync();
    }

    private async Task DisposeDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var serverConnectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = "master"
        };
        await DropDatabaseAsync(serverConnectionStringBuilder.ConnectionString, databaseName);
        databaseName = null;
    }

    public async Task<TestData> ResetAndSeedAsync()
    {
        RequireConfiguredConnection();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM [AssetEvents]; DELETE FROM [Alarms]; DELETE FROM [Assets];");

        var timestamp = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
        var healthy = new Asset
        {
            Name = "Boiler North",
            AssetCode = "AP-100",
            Type = "Boiler",
            Location = "North Plant",
            Temperature = 82.50m,
            Pressure = 220.00m,
            LastUpdated = timestamp
        };
        var warning = new Asset
        {
            Name = "Pump South",
            AssetCode = "AP-200",
            Type = "Pump",
            Location = "South Plant",
            Temperature = null,
            Pressure = 110.00m,
            LastUpdated = timestamp.AddMinutes(1)
        };
        var critical = new Asset
        {
            Name = "Pump North",
            AssetCode = "AP-300",
            Type = "Pump",
            Location = "North Plant",
            Temperature = 97.00m,
            Pressure = null,
            LastUpdated = timestamp.AddMinutes(2)
        };
        var noEvents = new Asset
        {
            Name = "Valve East",
            AssetCode = "AP-400",
            Type = "Valve",
            Location = "East Plant",
            Temperature = 18.00m,
            Pressure = 75.00m,
            LastUpdated = timestamp.AddMinutes(3)
        };
        dbContext.Assets.AddRange(healthy, warning, critical, noEvents);
        await dbContext.SaveChangesAsync();

        dbContext.Alarms.AddRange(
            new Alarm
            {
                AssetId = healthy.Id,
                Severity = AlarmSeverity.Critical,
                Message = "Resolved prior overpressure",
                Status = AlarmStatus.Resolved,
                CreatedAt = timestamp.AddMinutes(-20),
                ResolvedAt = timestamp.AddMinutes(-10)
            },
            new Alarm
            {
                AssetId = warning.Id,
                Severity = AlarmSeverity.Warning,
                Message = "Active low flow",
                Status = AlarmStatus.Active,
                CreatedAt = timestamp.AddMinutes(-5)
            },
            new Alarm
            {
                AssetId = critical.Id,
                Severity = AlarmSeverity.Critical,
                Message = "Acknowledged overpressure",
                Status = AlarmStatus.Acknowledged,
                CreatedAt = timestamp.AddMinutes(-2),
                AcknowledgedAt = timestamp.AddMinutes(-1)
            },
            new Alarm
            {
                AssetId = critical.Id,
                Severity = AlarmSeverity.Warning,
                Message = "Older warning",
                Status = AlarmStatus.Active,
                CreatedAt = timestamp.AddMinutes(-2)
            });
        dbContext.AssetEvents.AddRange(
            new AssetEvent
            {
                AssetId = warning.Id,
                PreviousStatus = AssetStatus.Healthy,
                NewStatus = AssetStatus.Warning,
                Timestamp = timestamp.AddMinutes(-5),
                Description = "Warning raised"
            },
            new AssetEvent
            {
                AssetId = critical.Id,
                PreviousStatus = AssetStatus.Warning,
                NewStatus = AssetStatus.Critical,
                Timestamp = timestamp.AddMinutes(-2),
                Description = "Critical raised"
            },
            new AssetEvent
            {
                AssetId = critical.Id,
                PreviousStatus = AssetStatus.Healthy,
                NewStatus = AssetStatus.Warning,
                Timestamp = timestamp.AddMinutes(-2),
                Description = "Warning raised first"
            });
        await dbContext.SaveChangesAsync();

        return new TestData(healthy.Id, warning.Id, critical.Id, noEvents.Id);
    }

    public AssetPulseDbContext CreateDbContext()
    {
        RequireConfiguredConnection();
        return new AssetPulseDbContext(new DbContextOptionsBuilder<AssetPulseDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AssetPulse"] = ConnectionString
            }));
    }

    private void RequireConfiguredConnection()
    {
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "ASSET_PULSE_TEST_CONNECTION must be configured before accessing the SQL Server API test database.");
        }
    }

    private static async Task CreateDatabaseAsync(string connectionString, string name)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string name)
    {
        if (!name.StartsWith(TestDatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to drop a database outside the Task 003 test naming convention.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
    }
}

public sealed record TestData(int HealthyAssetId, int WarningAssetId, int CriticalAssetId, int NoEventsAssetId);
