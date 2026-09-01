using System.Diagnostics;

namespace TopBar.Services;

internal sealed class SystemStatsService : IDisposable
{
    private readonly PerformanceCounter _cpuCounter;
    private PerformanceCounter? _diskCounter;
    private readonly List<PerformanceCounter> _netSentCounters = new();
    private readonly List<PerformanceCounter> _netRecvCounters = new();

    public SystemStatsService()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuCounter.NextValue();

        try
        {
            _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            _diskCounter.NextValue();
        }
        catch { _diskCounter = null; } // counter category can vary by Windows edition

        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            foreach (var instance in category.GetInstanceNames())
            {
                _netSentCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
                _netRecvCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
            }
            foreach (var c in _netSentCounters) c.NextValue();
            foreach (var c in _netRecvCounters) c.NextValue();
        }
        catch { /* leave lists empty — widget just reads 0 */ }
    }

    public float ReadCpuPercent() => _cpuCounter.NextValue();

    public float ReadDiskUsagePercent() => _diskCounter?.NextValue() ?? 0f;

    public (float downKbps, float upKbps) ReadNetworkKbps()
    {
        float down = 0, up = 0;
        foreach (var c in _netRecvCounters) down += c.NextValue();
        foreach (var c in _netSentCounters) up += c.NextValue();
        return (down / 1024f, up / 1024f);
    }

    public (float usedGb, float totalGb, float percent) ReadRam()
    {
        var status = new Win32.MEMORYSTATUSEX();
        if (!Win32.GlobalMemoryStatusEx(status))
            return (0, 0, 0);

        double totalGb = status.ullTotalPhys / 1024d / 1024d / 1024d;
        double availGb = status.ullAvailPhys / 1024d / 1024d / 1024d;
        double usedGb = totalGb - availGb;
        return ((float)usedGb, (float)totalGb, status.dwMemoryLoad);
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();
        _diskCounter?.Dispose();
        foreach (var c in _netSentCounters) c.Dispose();
        foreach (var c in _netRecvCounters) c.Dispose();
    }
}
