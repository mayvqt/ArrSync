using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public class WebhookIntegrationTests : IClassFixture<WebApplicationFactory<ArrSync.App.Program>>
{
    private readonly WebApplicationFactory<ArrSync.App.Program> _factory;

    public WebhookIntegrationTests(WebApplicationFactory<ArrSync.App.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // replace IOverseerClient with a fake that records calls
                services.AddSingleton<IOverseerClient, TestFakeOverseerClient>();
                // ensure config not dry-run
                services.Configure<ArrSync.App.Models.Config>(opts => { opts.DryRun = false; });
            });
        });
    }

    [Fact]
    public async Task SonarrWebhook_Post_Triggers_Delete()
    {
    var client = _factory.CreateClient();
    // Resolve the registered IOverseerClient and cast to the test implementation. This
    // makes the cast explicit so the nullable analyzer doesn't warn when we use it.
    var fake = (TestFakeOverseerClient)_factory.Services.GetRequiredService<IOverseerClient>();
    fake.NextMediaId = 9001;

        var payload = new
        {
            eventType = "SeriesDelete",
            instanceName = "sonarr",
            series = new { id = 2, title = "My Show", tmdbId = 101 }
        };

        var res = await client.PostAsync("/webhook/sonarr", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();

        // the CleanupService should call GetMediaIdByTmdbAsync and then DeleteMediaAsync
        fake.DeleteCalled.Should().BeTrue();
        fake.DeletedId.Should().Be(9001);
    }

    [Fact]
    public async Task RadarrWebhook_Post_Triggers_Delete()
    {
    var client = _factory.CreateClient();
    var fake = (TestFakeOverseerClient)_factory.Services.GetRequiredService<IOverseerClient>();
    fake.NextMediaId = 4242;

        var payload = new
        {
            eventType = "MovieDelete",
            instanceName = "radarr",
            movie = new { id = 1, title = "My Movie", tmdbId = 42 }
        };

        var res = await client.PostAsync("/webhook/radarr", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();

        fake.DeleteCalled.Should().BeTrue();
        fake.DeletedId.Should().Be(4242);
    }
}

internal class TestFakeOverseerClient : IOverseerClient
{
    public int? NextMediaId { get; set; } = 42;
    public bool DeleteCalled { get; private set; }
    public int DeletedId { get; private set; }

    public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct) => Task.FromResult((true, "ok"));

    public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct)
        => Task.FromResult(NextMediaId);

    public Task<bool> DeleteMediaAsync(int id, CancellationToken ct)
    {
        DeleteCalled = true;
        DeletedId = id;
        return Task.FromResult(true);
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
}
