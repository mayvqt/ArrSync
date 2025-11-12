using System;

namespace ArrSync.App.Services.Timing;

public interface IPeriodicTimerFactory
{
    IPeriodicTimer Create(TimeSpan interval);
}
