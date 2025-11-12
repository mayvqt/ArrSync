using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public class OverseerClientRetryTests
{
    [Fact]
    public async Task GetMediaIdByTmdb_RetriesOnTransientFailures_AndEventuallySucceeds()
    {
        int callCount = 0;
        using var handler = new MockHttpMessageHandler((req, ct) =>
        {
            callCount++;
            if (callCount < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("server error", Encoding.UTF8, "text/plain")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ \"id\": 999 }", Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost") };
    var opts = Options.Create(new ArrSync.App.Models.Config { MaxRetries = 5, InitialBackoffSeconds = 0 });
        var client = new OverseerClient(httpClient, opts, NullLogger<OverseerClient>.Instance);

        var id = await client.GetMediaIdByTmdbAsync(123, "movie", CancellationToken.None);
        id.Should().Be(999);
        callCount.Should().BeGreaterThanOrEqualTo(3);
    }
}
