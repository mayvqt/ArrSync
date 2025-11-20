using System.Net;
using System.Threading.Tasks;
using ArrSync.App.Services.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ArrSync.Tests;

public class OverseerHttpIntegrationTests {
    [Fact]
    public async Task OverseerHttp_GetHealth_ReturnsOk() {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web => {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddRouting());
                web.Configure(app => {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => {
                        endpoints.MapGet("/api/v1/status", async ctx => await ctx.Response.WriteAsync("ok"));
                    });
                });
            })
            .StartAsync();

        var client = host.GetTestClient();
        var overseer = new OverseerHttp(client);

        var resp = await overseer.GetAsync("/api/v1/status", default);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var text = await resp.Content.ReadAsStringAsync();
        Assert.Equal("ok", text);
    }
}
