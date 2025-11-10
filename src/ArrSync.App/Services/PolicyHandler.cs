using Polly;

namespace ArrSync.App.Services;

public class PolicyHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public PolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _policy = policy;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Execute the HTTP call through the provided Polly policy
        return _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
    }
}