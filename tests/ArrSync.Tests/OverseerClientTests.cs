using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

internal class DelegateHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responder(request));
    }
}

public class OverseerClientTests
{
    private static OverseerClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new DelegateHandler(responder);
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://overseer")
        };

        var cfg = Options.Create(new Config { MaxRetries = 1, InitialBackoffSeconds = 1 });
        var logger = NullLogger<OverseerClient>.Instance;
        var overseerHttp = new ArrSync.App.Services.Http.OverseerHttp(http);
        return new OverseerClient(overseerHttp, cfg, logger);
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk_WhenStatus200()
    {
        var client = CreateClient(req => new HttpResponseMessage(HttpStatusCode.OK));
        var (ok, details) = await client.HealthCheckAsync(CancellationToken.None);
        Assert.True(ok);
        Assert.Equal("ok", details);
    }

    [Fact]
    public async Task GetMediaIdByTmdb_ReturnsId_WhenJsonHasId()
    {
        var json = "{ \"id\": 123 }";
        var client = CreateClient(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var id = await client.GetMediaIdByTmdbAsync(555, "movie", CancellationToken.None);
        Assert.Equal(123, id);
    }

    [Fact]
    public async Task DeleteMedia_ReturnsTrue_OnSuccess()
    {
        var client = CreateClient(req => new HttpResponseMessage(HttpStatusCode.OK));
        var ok = await client.DeleteMediaAsync(42, CancellationToken.None);
        Assert.True(ok);
    }
}
