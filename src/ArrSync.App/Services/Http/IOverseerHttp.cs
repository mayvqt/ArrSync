namespace ArrSync.App.Services.Http;

public interface IOverseerHttp {
    Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct);
}
