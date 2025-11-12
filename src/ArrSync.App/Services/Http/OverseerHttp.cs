using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.App.Services.Http;

public sealed class OverseerHttp : IOverseerHttp
{
    private readonly HttpClient _client;

    public OverseerHttp(HttpClient client)
    {
        _client = client;
    }

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
        => _client.GetAsync(url, ct);

    public Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct)
        => _client.DeleteAsync(url, ct);
}
