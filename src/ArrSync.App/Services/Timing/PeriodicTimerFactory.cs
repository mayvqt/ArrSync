namespace ArrSync.App.Services.Timing;

public sealed class PeriodicTimerFactory : IPeriodicTimerFactory
{
    public IPeriodicTimer Create(TimeSpan interval)
    {
        return new PeriodicTimerWrapper(new PeriodicTimer(interval));
    }
}

internal sealed class PeriodicTimerWrapper : IPeriodicTimer
{
    private readonly PeriodicTimer _timer;

    public PeriodicTimerWrapper(PeriodicTimer timer)
    {
        _timer = timer;
    }

    public async Task<bool> WaitForNextTickAsync(CancellationToken ct)
    {
        try
        {
            return await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
