using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using ArrSync.App.Services.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ArrSync.App.Endpoints;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests.Endpoints;

public class EndpointsIntegrationTests
{
    private record WebhookResponseDto(string Message);

    [Fact]
    public async Task Health_Returns200_WhenOverseerOk()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IOverseerClient>(new FakeOverseer(true));

        var app = builder.Build();
        app.MapHealthEndpoint();
        await app.StartAsync();

    var client = app.GetTestClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Health_Returns503_WhenOverseerDown()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IOverseerClient>(new FakeOverseer(false));

        var app = builder.Build();
        app.MapHealthEndpoint();
        await app.StartAsync();

    var client = app.GetTestClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
    }

    [Fact]
    public async Task RadarrWebhook_RejectsWhenSecretMissing()
    {
        var cfg = new Config { WebhookSecret = "supersecret" };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(Options.Create(cfg));
        builder.Services.AddSingleton<ICleanupService>(new FakeCleanup());

        var app = builder.Build();
        app.MapRadarrWebhookEndpoint();
        await app.StartAsync();

    var client = app.GetTestClient();
        var payload = new RadarrWebhook { EventType = "Delete", Movie = new RadarrMovie { TmdbId = 123 } };
        var res = await client.PostAsJsonAsync("/webhook/radarr", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task RadarrWebhook_IgnoresTestEvents()
    {
        var cfg = new Config { WebhookSecret = null };
        var fake = new FakeCleanup();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(Options.Create(cfg));
        builder.Services.AddSingleton<ICleanupService>(fake);

        var app = builder.Build();
        app.MapRadarrWebhookEndpoint();
        await app.StartAsync();

    var client = app.GetTestClient();
        var payload = new RadarrWebhook { EventType = "Test" };
        var res = await client.PostAsJsonAsync("/webhook/radarr", payload);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<WebhookResponseDto>();
        Assert.Equal("test event ignored", body!.Message);
        Assert.False(fake.Processed);
    }

    [Fact]
    public async Task RadarrWebhook_ProcessesWhenValid()
    {
        var cfg = new Config { WebhookSecret = null };
        var fake = new FakeCleanup();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(Options.Create(cfg));
        builder.Services.AddSingleton<ICleanupService>(fake);

        var app = builder.Build();
        app.MapRadarrWebhookEndpoint();
        await app.StartAsync();

    var client = app.GetTestClient();
        var payload = new RadarrWebhook { EventType = "Delete", Movie = new RadarrMovie { TmdbId = 321 } };
        var res = await client.PostAsJsonAsync("/webhook/radarr", payload);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<WebhookResponseDto>();
        Assert.Equal("processed", body!.Message);
        Assert.True(fake.Processed);
    }

    private class FakeOverseer : IOverseerClient
    {
        private readonly bool _ok;
        public FakeOverseer(bool ok) => _ok = ok;
        public Task<bool> IsAvailableAsync() => Task.FromResult(_ok);
        public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct) => Task.FromResult((_ok, _ok ? "ok" : "down"));
        public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct) => Task.FromResult<int?>(null);
        public Task<bool> DeleteMediaAsync(int id, CancellationToken ct) => Task.FromResult(true);
    }

    private class FakeCleanup : ICleanupService
    {
        public bool Processed { get; private set; }
        public Task ProcessSonarrAsync(int tmdbId, CancellationToken ct) { Processed = true; return Task.CompletedTask; }
        public Task ProcessRadarrAsync(int tmdbId, CancellationToken ct) { Processed = true; return Task.CompletedTask; }
    }
}
