namespace ArrSync.App.Services.Http;

public sealed class OverseerHttp : IOverseerHttp
{
    private readonly HttpClient _client;

    public OverseerHttp(HttpClient client)
    {
        _client = client;
    }

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
    {
        return _client.GetAsync(url, ct);
    }

    public Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct)
    {
        return _client.DeleteAsync(url, ct);
    }
}
