using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetPulse.Api.Contracts;
using AssetPulse.Api.Domain;
using AssetPulse.Api.Services;
using AssetPulse.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssetPulse.Api.Tests;

public sealed class ReadApiIntegrationTests : IClassFixture<SqlServerApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqlServerApiFixture fixture;

    public ReadApiIntegrationTests(SqlServerApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [SkippableFact]
    public async Task Assets_support_filters_status_projection_ordering_and_pagination()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var all = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets");
        Assert.Equal(4, all.TotalCount);
        Assert.Equal(20, all.PageSize);
        Assert.Equal(["AP-100", "AP-200", "AP-300", "AP-400"], all.Items.Select(asset => asset.AssetCode));
        Assert.Equal(AssetStatus.Healthy, all.Items.Single(asset => asset.AssetCode == "AP-100").Status);
        Assert.Equal(AssetStatus.Warning, all.Items.Single(asset => asset.AssetCode == "AP-200").Status);
        Assert.Equal(AssetStatus.Critical, all.Items.Single(asset => asset.AssetCode == "AP-300").Status);
        var rawAsset = await client.GetStringAsync("/api/assets");
        Assert.DoesNotContain("alarms", rawAsset, StringComparison.OrdinalIgnoreCase);

        var search = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?search=%20Pump%20");
        Assert.Equal(2, search.TotalCount);
        var type = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?type=%20Pump%20");
        Assert.Equal(2, type.TotalCount);
        var location = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?location=North%20Plant");
        Assert.Equal(2, location.TotalCount);
        var status = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?status=Warning");
        Assert.Equal(["AP-200"], status.Items.Select(asset => asset.AssetCode));
        var combined = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?type=Pump&location=North%20Plant&status=Critical");
        Assert.Equal(["AP-300"], combined.Items.Select(asset => asset.AssetCode));
        var page = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?page=2&pageSize=3");
        Assert.Equal(["AP-400"], page.Items.Select(asset => asset.AssetCode));
        var beyond = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?page=99&pageSize=3");
        Assert.Empty(beyond.Items);
    }

    [SkippableTheory]
    [InlineData("/api/assets?page=0")]
    [InlineData("/api/assets?pageSize=0")]
    [InlineData("/api/assets?pageSize=101")]
    [InlineData("/api/assets?status=99")]
    [InlineData("/api/alarms?assetId=0")]
    [InlineData("/api/alarms?severity=99")]
    [InlineData("/api/alarms?status=99")]
    public async Task Invalid_query_values_return_validation_problem_details(string url)
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync(url);
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(document.RootElement.TryGetProperty("errors", out _));
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
    }

    [SkippableFact]
    public async Task Asset_detail_returns_dto_with_status_and_missing_asset_returns_problem_details()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync($"/api/assets/{data.CriticalAssetId}");
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(JsonOptions);
        var missing = await client.GetAsync("/api/assets/999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(asset);
        Assert.Equal(AssetStatus.Critical, asset.Status);
        Assert.Equal("AP-300", asset.AssetCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [SkippableFact]
    public async Task Asset_events_require_existing_parent_and_are_newest_first_and_paginated()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var events = await GetAsync<PagedResponse<AssetEventResponse>>(client, $"/api/assets/{data.CriticalAssetId}/events?pageSize=1");
        var empty = await GetAsync<PagedResponse<AssetEventResponse>>(client, $"/api/assets/{data.NoEventsAssetId}/events");
        var missing = await client.GetAsync("/api/assets/999999/events");

        Assert.Equal(2, events.TotalCount);
        Assert.Single(events.Items);
        Assert.Equal("Warning raised first", events.Items[0].Description);
        Assert.Empty(empty.Items);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [SkippableFact]
    public async Task Alarms_support_filters_ordering_pagination_and_empty_results()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        var data = await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var all = await GetAsync<PagedResponse<AlarmResponse>>(client, "/api/alarms");
        var sameTime = all.Items.Where(alarm => alarm.CreatedAt == all.Items[0].CreatedAt).ToList();
        var filtered = await GetAsync<PagedResponse<AlarmResponse>>(
            client,
            $"/api/alarms?assetId={data.CriticalAssetId}&severity=Critical&status=Acknowledged");
        var warning = await GetAsync<PagedResponse<AlarmResponse>>(client, "/api/alarms?severity=Warning");
        var page = await GetAsync<PagedResponse<AlarmResponse>>(client, "/api/alarms?page=2&pageSize=3");
        var noMatch = await GetAsync<PagedResponse<AlarmResponse>>(client, "/api/alarms?assetId=999999");

        Assert.Equal(4, all.TotalCount);
        Assert.True(sameTime.Zip(sameTime.Skip(1)).All(pair => pair.First.Id > pair.Second.Id));
        Assert.Single(filtered.Items);
        Assert.Equal("AP-300", filtered.Items[0].AssetCode);
        Assert.Equal(2, warning.TotalCount);
        Assert.Single(page.Items);
        Assert.Empty(noMatch.Items);
    }

    [SkippableFact]
    public async Task Whitespace_search_and_valid_no_match_return_successful_empty_page()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        using var client = fixture.CreateClient();

        var whitespace = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?search=%20%20%20");
        var noMatch = await GetAsync<PagedResponse<AssetResponse>>(client, "/api/assets?search=does-not-exist");

        Assert.Equal(4, whitespace.TotalCount);
        Assert.Empty(noMatch.Items);
        Assert.Equal(0, noMatch.TotalCount);
    }

    [SkippableFact]
    public async Task Unexpected_exceptions_return_safe_problem_details_without_exception_text()
    {
        Skip.If(!fixture.IsConfigured, "Set ASSET_PULSE_TEST_CONNECTION to run SQL Server-backed API integration tests.");
        await fixture.ResetAndSeedAsync();
        await using var failingFactory = new FailingAssetReadFactory(fixture.ConnectionString);
        using var client = failingFactory.CreateClient();

        var response = await client.GetAsync("/api/assets");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("unexpected error", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(FailingAssetReadService.ExceptionMessage, body, StringComparison.Ordinal);
        Assert.Contains("traceId", body, StringComparison.Ordinal);
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return Assert.IsType<T>(result);
    }
}

internal sealed class FailingAssetReadFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AssetPulse"] = connectionString
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAssetReadService>();
            services.AddScoped<IAssetReadService, FailingAssetReadService>();
        });
    }
}

internal sealed class FailingAssetReadService : IAssetReadService
{
    public const string ExceptionMessage = "test-only sensitive failure detail";

    public Task<PagedResponse<AssetResponse>> GetAssetsAsync(
        string? search,
        string? type,
        string? location,
        AssetStatus? status,
        Pagination pagination,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException(ExceptionMessage);

    public Task<AssetResponse?> GetAssetAsync(int id, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(ExceptionMessage);
}
