using Polly;

namespace ArrSync.App.Services.Http;

public class PolicyHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public PolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return await _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken).ConfigureAwait(false);
    }
}
