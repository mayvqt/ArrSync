using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.App.Services.Timing;

public interface IPeriodicTimer : IDisposable
{
    /// <summary>
    /// Waits for the next tick. Returns true if the timer ticked, false if it was cancelled.
    /// </summary>
    Task<bool> WaitForNextTickAsync(CancellationToken ct);
}
