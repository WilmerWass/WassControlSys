using System;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IMonitoringService : IDisposable
    {
        Task<SystemUsage> GetSystemUsageAsync();
        TimeSpan GetIdleTime();
    }
}
