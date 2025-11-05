using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PerformanceWatcher
{
    public class ProcessMetrics
    {
        public string ProcessName { get; set; } = "";
        public List<int> PIDs { get; set; } = new List<int>();
        public double CpuPercent { get; set; }
        public long MemoryBytes { get; set; }
        public long NetworkUploadBytes { get; set; }
        public long NetworkDownloadBytes { get; set; }
        public DateTime Timestamp { get; set; }

        public double MemoryMB => MemoryBytes / (1024.0 * 1024.0);
        public double NetworkUploadKBps { get; set; }
        public double NetworkDownloadKBps { get; set; }
    }

    public class ProcessMonitor : IDisposable
    {
        private readonly Dictionary<int, PerformanceCounter> _cpuCounters = new();
        private readonly Dictionary<int, DateTime> _lastCpuCheck = new();
        private readonly Dictionary<int, TimeSpan> _lastCpuTime = new();
        private readonly object _lock = new object();
        private static readonly int ProcessorCount = Environment.ProcessorCount;

        public ProcessMetrics GetProcessMetrics(string processName, List<int> pids)
        {
            var metrics = new ProcessMetrics
            {
                ProcessName = processName,
                PIDs = new List<int>(pids),
                Timestamp = DateTime.Now
            };

            double totalCpu = 0;
            long totalMemory = 0;

            lock (_lock)
            {
                var validPids = new List<int>();

                foreach (var pid in pids)
                {
                    try
                    {
                        var process = Process.GetProcessById(pid);

                        // CPU Calculation - using manual calculation for better performance
                        var currentTime = DateTime.Now;
                        var currentCpuTime = process.TotalProcessorTime;

                        if (_lastCpuCheck.ContainsKey(pid) && _lastCpuTime.ContainsKey(pid))
                        {
                            var timeDiff = (currentTime - _lastCpuCheck[pid]).TotalMilliseconds;
                            var cpuDiff = (currentCpuTime - _lastCpuTime[pid]).TotalMilliseconds;

                            if (timeDiff > 0)
                            {
                                var cpuUsage = (cpuDiff / (ProcessorCount * timeDiff)) * 100.0;
                                totalCpu += Math.Min(cpuUsage, 100.0); // Cap at 100% per process
                            }
                        }

                        _lastCpuCheck[pid] = currentTime;
                        _lastCpuTime[pid] = currentCpuTime;

                        // Memory
                        totalMemory += process.WorkingSet64;

                        validPids.Add(pid);
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists
                        CleanupPid(pid);
                    }
                    catch (Exception)
                    {
                        // Other errors, skip this PID
                    }
                }

                metrics.PIDs = validPids;
            }

            metrics.CpuPercent = totalCpu;
            metrics.MemoryBytes = totalMemory;

            return metrics;
        }

        public List<Process> SearchProcessesByName(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Process>();

            try
            {
                var allProcesses = Process.GetProcesses();
                return allProcesses
                    .Where(p => p.ProcessName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(p => p.ProcessName)
                    .ToList();
            }
            catch
            {
                return new List<Process>();
            }
        }

        private void CleanupPid(int pid)
        {
            if (_cpuCounters.ContainsKey(pid))
            {
                _cpuCounters[pid]?.Dispose();
                _cpuCounters.Remove(pid);
            }
            _lastCpuCheck.Remove(pid);
            _lastCpuTime.Remove(pid);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var counter in _cpuCounters.Values)
                {
                    counter?.Dispose();
                }
                _cpuCounters.Clear();
                _lastCpuCheck.Clear();
                _lastCpuTime.Clear();
            }
        }
    }
}
