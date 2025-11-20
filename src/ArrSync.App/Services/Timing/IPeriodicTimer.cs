namespace ArrSync.App.Services.Timing;

public interface IPeriodicTimer : IDisposable
{
    Task<bool> WaitForNextTickAsync(CancellationToken ct);
}
