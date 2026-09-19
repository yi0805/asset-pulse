using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetPulse.Api.Contracts;
using AssetPulse.Api.Domain;
using AssetPulse.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AssetPulse.Api.Tests;

public sealed class AssetWriteIntegrationTests(SqlServerApiFixture fixture) : IClassFixture<SqlServerApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [SkippableFact]
    public async Task Create_normalizes_persists_and_writes_one_initial_healthy_event()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();
        var before = DateTimeOffset.UtcNow;

        var response = await client.PostAsJsonAsync("/api/assets", new AssetUpsertRequest
        {
            Name = "  Compressor one  ",
            AssetCode = "  comp-001  ",
            Type = "  Compressor ",
            Location = "  West plant ",
            Temperature = 42.5m,
            Pressure = 0m
        });
        var created = await response.Content.ReadFromJsonAsync<AssetResponse>(JsonOptions);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal($"/api/assets/{created.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Compressor one", created.Name);
        Assert.Equal("COMP-001", created.AssetCode);
        Assert.Equal("Compressor", created.Type);
        Assert.Equal("West plant", created.Location);
        Assert.Equal(AssetStatus.Healthy, created.Status);
        Assert.InRange(created.LastUpdated, before, after);

        await using var dbContext = fixture.CreateDbContext();
        var persisted = await dbContext.Assets.SingleAsync(asset => asset.Id == created.Id);
        var events = await dbContext.AssetEvents.Where(assetEvent => assetEvent.AssetId == created.Id).ToListAsync();
        var initialEvent = Assert.Single(events);
        Assert.Equal("COMP-001", persisted.AssetCode);
        Assert.Equal(created.LastUpdated, persisted.LastUpdated);
        Assert.Null(initialEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Healthy, initialEvent.NewStatus);
        Assert.Equal(created.LastUpdated, initialEvent.Timestamp);
        Assert.Equal("Asset created", initialEvent.Description);
    }

    [SkippableTheory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_rejects_invalid_asset_fields(AssetUpsertRequest request, string field)
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/assets", request);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [SkippableFact]
    public async Task Create_returns_asset_code_conflict_for_normalized_duplicate_without_changing_existing_asset()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();
        var original = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/{data.HealthyAssetId}", JsonOptions);

        var response = await client.PostAsJsonAsync("/api/assets", ValidRequest("  ap-100  "));
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var unchanged = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/{data.HealthyAssetId}", JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("assetCode", out _));
        Assert.Equal(original, unchanged);
    }

    [SkippableFact]
    public async Task Update_persists_normalized_changes_preserves_derived_status_and_does_not_create_status_event()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Alarms.Add(new Alarm
            {
                AssetId = data.HealthyAssetId,
                Severity = AlarmSeverity.Warning,
                Status = AlarmStatus.Active,
                Message = "Status must remain derived",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = fixture.CreateClient();
        var original = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/{data.HealthyAssetId}", JsonOptions);
        var eventsBefore = await client.GetFromJsonAsync<PagedResponse<AssetEvent>>(
            $"/api/assets/{data.HealthyAssetId}/events", JsonOptions);
        var response = await client.PutAsJsonAsync(
            $"/api/assets/{data.HealthyAssetId}",
            ValidRequest(" ap-101 ") with { Name = "  Updated boiler  ", Temperature = null, Pressure = 0m });
        var updated = await response.Content.ReadFromJsonAsync<AssetResponse>(JsonOptions);
        var eventsAfter = await client.GetFromJsonAsync<PagedResponse<AssetEvent>>(
            $"/api/assets/{data.HealthyAssetId}/events", JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated boiler", updated.Name);
        Assert.Equal("AP-101", updated.AssetCode);
        Assert.Null(updated.Temperature);
        Assert.Equal(0m, updated.Pressure);
        Assert.Equal(AssetStatus.Warning, updated.Status);
        Assert.NotEqual(original!.LastUpdated, updated.LastUpdated);
        Assert.Equal(eventsBefore!.TotalCount, eventsAfter!.TotalCount);

        await using var freshContext = fixture.CreateDbContext();
        var persisted = await freshContext.Assets.AsNoTracking().SingleAsync(asset => asset.Id == data.HealthyAssetId);
        Assert.Equal("AP-101", persisted.AssetCode);
        Assert.Equal(updated.LastUpdated, persisted.LastUpdated);
    }

    [SkippableFact]
    public async Task Update_identical_request_preserves_timestamp_and_handles_missing_and_duplicate_codes()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();
        var original = await client.GetFromJsonAsync<AssetResponse>($"/api/assets/{data.HealthyAssetId}", JsonOptions);
        var identical = await client.PutAsJsonAsync($"/api/assets/{data.HealthyAssetId}", new AssetUpsertRequest
        {
            Name = $"  {original!.Name}  ",
            AssetCode = original.AssetCode.ToLowerInvariant(),
            Type = original.Type,
            Location = original.Location,
            Temperature = original.Temperature,
            Pressure = original.Pressure
        });
        var identicalResult = await identical.Content.ReadFromJsonAsync<AssetResponse>(JsonOptions);
        var missing = await client.PutAsJsonAsync("/api/assets/999999", ValidRequest("UNUSED-001"));
        var conflict = await client.PutAsJsonAsync($"/api/assets/{data.HealthyAssetId}", ValidRequest("AP-200"));

        Assert.Equal(HttpStatusCode.OK, identical.StatusCode);
        Assert.Equal(original.LastUpdated, identicalResult!.LastUpdated);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        yield return [ValidRequest("VALID-001") with { Name = "   " }, "name"];
        yield return [ValidRequest("VALID-001") with { AssetCode = new string('A', 33) }, "assetCode"];
        yield return [ValidRequest("VALID-001") with { Type = new string('T', 51) }, "type"];
        yield return [ValidRequest("VALID-001") with { Location = new string('L', 101) }, "location"];
        yield return [ValidRequest("VALID-001") with { Temperature = -273.16m }, "temperature"];
        yield return [ValidRequest("VALID-001") with { Pressure = -0.01m }, "pressure"];
        yield return [ValidRequest("VALID-001") with { Pressure = 1.234m }, "pressure"];
    }

    private static AssetUpsertRequest ValidRequest(string assetCode) => new()
    {
        Name = "Valid asset",
        AssetCode = assetCode,
        Type = "Pump",
        Location = "Test plant",
        Temperature = 20m,
        Pressure = 100m
    };
}
