using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Services.Timing;

namespace ArrSync.Tests.TestUtils;

public class FakePeriodicTimer : IPeriodicTimer {
    private readonly SemaphoreSlim _sem = new(0, int.MaxValue);
    private bool _disposed;

    public async Task<bool> WaitForNextTickAsync(CancellationToken ct) {
        try {
            await _sem.WaitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _sem.Dispose();
    }

    public void Tick() {
        _sem.Release();
    }
}

public class FakePeriodicTimerFactory : IPeriodicTimerFactory {
    private readonly ConcurrentQueue<TimeSpan> _intervals = new();
    private readonly ConcurrentQueue<FakePeriodicTimer> _queue = new();
    private readonly ConcurrentBag<TimeSpan> _recorded = new();

    public FakePeriodicTimer[] CreatedTimers => _queue.ToArray();
    public TimeSpan[] Intervals => _intervals.ToArray();
    public TimeSpan[] RecordedIntervals => _recorded.ToArray();

    public IPeriodicTimer Create(TimeSpan interval) {
        var t = new FakePeriodicTimer();
        _queue.Enqueue(t);
        _intervals.Enqueue(interval);
        _recorded.Add(interval);
        return t;
    }

    public bool TickNext() {
        if (_queue.TryDequeue(out var t)) {
            t.Tick();
            return true;
        }

        return false;
    }

    public void TickAll() {
        while (TickNext()) {
        }
    }
}
