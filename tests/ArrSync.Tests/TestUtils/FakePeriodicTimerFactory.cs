using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.Tests.TestUtils;

public class FakePeriodicTimer : ArrSync.App.Services.Timing.IPeriodicTimer
{
    private readonly SemaphoreSlim _sem = new(0, int.MaxValue);
    private bool _disposed;

    public void Tick() => _sem.Release();

    public async Task<bool> WaitForNextTickAsync(CancellationToken ct)
    {
        try
        {
            await _sem.WaitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sem.Dispose();
    }
}

public class FakePeriodicTimerFactory : ArrSync.App.Services.Timing.IPeriodicTimerFactory
{
    private readonly ConcurrentQueue<FakePeriodicTimer> _queue = new();
    private readonly ConcurrentQueue<TimeSpan> _intervals = new();
    private readonly ConcurrentBag<TimeSpan> _recorded = new();

    public FakePeriodicTimer[] CreatedTimers => _queue.ToArray();
    public TimeSpan[] Intervals => _intervals.ToArray();
    public TimeSpan[] RecordedIntervals => _recorded.ToArray();

    public ArrSync.App.Services.Timing.IPeriodicTimer Create(TimeSpan interval)
    {
        var t = new FakePeriodicTimer();
        _queue.Enqueue(t);
        _intervals.Enqueue(interval);
        _recorded.Add(interval);
        return t;
    }

    /// <summary>
    /// Tick the next created timer (FIFO). Returns false if none available.
    /// </summary>
    public bool TickNext()
    {
        if (_queue.TryDequeue(out var t))
        {
            t.Tick();
            return true;
        }
        return false;
    }

    public void TickAll()
    {
        while (TickNext()) { }
    }
}
