using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.App.Services.Http;

/// <summary>
/// Abstraction around HTTP operations used to communicate with Overseerr.
/// Extracted to allow easier unit testing and to decouple retry/timeout logic from the client.
/// </summary>
public interface IOverseerHttp
{
    Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct);
}
