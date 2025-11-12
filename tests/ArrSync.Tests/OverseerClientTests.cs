using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ArrSync.App.Services;
using ArrSync.App.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public class OverseerClientTests
{
    [Fact]
    public async Task HealthCheck_ReturnsOk_When_Status200()
    {
        using var handler = new MockHttpMessageHandler((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"status\": \"ok\" }", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        });

        var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost") };
    var opts = Options.Create(new ArrSync.App.Models.Config { MaxRetries = 1, InitialBackoffSeconds = 1 });
        var client = new OverseerClient(httpClient, opts, NullLogger<OverseerClient>.Instance);

        var (ok, detail) = await client.HealthCheckAsync(CancellationToken.None);
        ok.Should().BeTrue();
        detail.Should().Contain("ok");
    }

    [Fact]
    public async Task GetMediaIdByTmdb_ReturnsId_When_MediaInfoPresent()
    {
        var json = "{ \"mediaInfo\": { \"id\": 42 } }";
        using var handler = new MockHttpMessageHandler((req, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        });

        var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost") };
    var opts = Options.Create(new ArrSync.App.Models.Config { MaxRetries = 1, InitialBackoffSeconds = 1 });
        var client = new OverseerClient(httpClient, opts, NullLogger<OverseerClient>.Instance);

        var id = await client.GetMediaIdByTmdbAsync(123, "movie", CancellationToken.None);
        id.Should().Be(42);
    }
}

// Simple Test helpers
internal class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public TestHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }
}

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}
