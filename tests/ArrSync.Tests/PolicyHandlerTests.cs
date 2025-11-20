using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Services.Http;
using Polly;
using Xunit;

namespace ArrSync.Tests;

internal class SimpleHandler : HttpMessageHandler {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public SimpleHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) {
        return Task.FromResult(_responder(request));
    }
}

public class PolicyHandlerTests {
    [Fact]
    public async Task PolicyHandler_InvokesInnerHandler() {
        var inner = new SimpleHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var policy = Policy.NoOpAsync<HttpResponseMessage>();
        var ph = new PolicyHandler(policy) { InnerHandler = inner };

        using var invoker = new HttpMessageInvoker(ph);
        var resp = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test"),
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
