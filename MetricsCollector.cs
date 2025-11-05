using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceWatcher
{
    public class MonitoredProcess
    {
        public string ProcessName { get; set; } = "";
        public List<int> PIDs { get; set; } = new List<int>();
        public bool IsActive { get; set; } = true;
    }

    public class MetricsCollector : IDisposable
    {
        private readonly ProcessMonitor _processMonitor;
        private readonly NetworkMonitor _networkMonitor;
        private readonly Dictionary<string, MonitoredProcess> _monitoredProcesses = new();
        private readonly Dictionary<string, List<ProcessMetrics>> _historicalData = new();
        private readonly object _lock = new object();

        public MetricsCollector()
        {
            _processMonitor = new ProcessMonitor();
            _networkMonitor = new NetworkMonitor();
        }

        public void AddProcess(string processName, List<int> pids)
        {
            lock (_lock)
            {
                if (_monitoredProcesses.ContainsKey(processName))
                {
                    // Add new PIDs to existing process
                    var existing = _monitoredProcesses[processName];
                    foreach (var pid in pids)
                    {
                        if (!existing.PIDs.Contains(pid))
                            existing.PIDs.Add(pid);
                    }
                }
                else
                {
                    _monitoredProcesses[processName] = new MonitoredProcess
                    {
                        ProcessName = processName,
                        PIDs = new List<int>(pids),
                        IsActive = true
                    };
                    _historicalData[processName] = new List<ProcessMetrics>();
                }
            }
        }

        public void RemoveProcess(string processName)
        {
            lock (_lock)
            {
                if (_monitoredProcesses.ContainsKey(processName))
                {
                    var pids = _monitoredProcesses[processName].PIDs;
                    foreach (var pid in pids)
                    {
                        _networkMonitor.CleanupProcess(pid);
                    }
                    _monitoredProcesses.Remove(processName);
                }
            }
        }

        public List<ProcessMetrics> CollectMetrics()
        {
            var results = new List<ProcessMetrics>();

            lock (_lock)
            {
                var processesToRemove = new List<string>();

                foreach (var kvp in _monitoredProcesses.ToList())
                {
                    var processName = kvp.Key;
                    var monitoredProcess = kvp.Value;

                    if (!monitoredProcess.IsActive)
                        continue;

                    // Get CPU and Memory metrics
                    var metrics = _processMonitor.GetProcessMetrics(processName, monitoredProcess.PIDs);

                    // Get Network metrics for each PID and aggregate
                    double totalDownloadKBps = 0;
                    double totalUploadKBps = 0;

                    var validPids = new List<int>();
                    foreach (var pid in monitoredProcess.PIDs)
                    {
                        var (downloadBytes, uploadBytes, downloadKBps, uploadKBps) =
                            _networkMonitor.GetProcessNetworkStats(pid);

                        totalDownloadKBps += downloadKBps;
                        totalUploadKBps += uploadKBps;

                        // Check if PID still exists in metrics
                        if (metrics.PIDs.Contains(pid))
                            validPids.Add(pid);
                    }

                    // Update PIDs list with valid ones
                    monitoredProcess.PIDs = validPids;
                    metrics.PIDs = validPids;

                    if (validPids.Count == 0)
                    {
                        // All PIDs are gone, mark for removal
                        processesToRemove.Add(processName);
                        continue;
                    }

                    metrics.NetworkDownloadKBps = totalDownloadKBps;
                    metrics.NetworkUploadKBps = totalUploadKBps;

                    // Store in historical data (keep last 3600 seconds = 1 hour)
                    if (!_historicalData.ContainsKey(processName))
                        _historicalData[processName] = new List<ProcessMetrics>();

                    _historicalData[processName].Add(metrics);

                    // Keep only recent data (e.g., last 3600 entries for 1 hour at 1 Hz)
                    if (_historicalData[processName].Count > 3600)
                        _historicalData[processName].RemoveAt(0);

                    results.Add(metrics);
                }

                // Remove processes with no active PIDs
                foreach (var processName in processesToRemove)
                {
                    RemoveProcess(processName);
                }
            }

            return results;
        }

        public List<ProcessMetrics> GetHistoricalData(string processName, int maxSeconds = 600)
        {
            lock (_lock)
            {
                if (_historicalData.ContainsKey(processName))
                {
                    var data = _historicalData[processName];
                    return data.Skip(Math.Max(0, data.Count - maxSeconds)).ToList();
                }
                return new List<ProcessMetrics>();
            }
        }

        public List<ProcessMetrics> GetAggregatedHistoricalData(int maxSeconds = 600)
        {
            lock (_lock)
            {
                // Aggregate all processes data by timestamp
                var aggregated = new Dictionary<DateTime, ProcessMetrics>();

                foreach (var kvp in _historicalData)
                {
                    var data = kvp.Value.Skip(Math.Max(0, kvp.Value.Count - maxSeconds));

                    foreach (var metrics in data)
                    {
                        var timestamp = metrics.Timestamp;
                        if (!aggregated.ContainsKey(timestamp))
                        {
                            aggregated[timestamp] = new ProcessMetrics
                            {
                                ProcessName = "Total",
                                Timestamp = timestamp
                            };
                        }

                        var agg = aggregated[timestamp];
                        agg.CpuPercent += metrics.CpuPercent;
                        agg.MemoryBytes += metrics.MemoryBytes;
                        agg.NetworkDownloadKBps += metrics.NetworkDownloadKBps;
                        agg.NetworkUploadKBps += metrics.NetworkUploadKBps;
                    }
                }

                return aggregated.Values.OrderBy(m => m.Timestamp).ToList();
            }
        }

        public List<string> GetMonitoredProcessNames()
        {
            lock (_lock)
            {
                return _monitoredProcesses.Keys.ToList();
            }
        }

        public MonitoredProcess? GetMonitoredProcess(string processName)
        {
            lock (_lock)
            {
                return _monitoredProcesses.ContainsKey(processName)
                    ? _monitoredProcesses[processName]
                    : null;
            }
        }

        public void Dispose()
        {
            _processMonitor?.Dispose();
            _networkMonitor?.Dispose();

            lock (_lock)
            {
                _monitoredProcesses.Clear();
                _historicalData.Clear();
            }
        }
    }
}
