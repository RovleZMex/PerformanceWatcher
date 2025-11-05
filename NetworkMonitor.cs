using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace PerformanceWatcher
{
    public class NetworkMonitor : IDisposable
    {
        private readonly Dictionary<int, long> _lastBytesReceived = new();
        private readonly Dictionary<int, long> _lastBytesSent = new();
        private readonly Dictionary<int, DateTime> _lastCheck = new();
        private readonly object _lock = new object();

        // For system-wide network monitoring as fallback
        private long _lastTotalBytesReceived;
        private long _lastTotalBytesSent;
        private DateTime _lastTotalCheck = DateTime.Now;

        public (long downloadBytes, long uploadBytes, double downloadKBps, double uploadKBps) GetProcessNetworkStats(int pid)
        {
            lock (_lock)
            {
                try
                {
                    // Try to get per-process network stats using Performance Counters
                    // Note: This requires elevated privileges and proper counter configuration
                    var process = Process.GetProcessById(pid);
                    var processName = process.ProcessName;

                    // For Windows, we can try to use ".NET CLR Networking" performance counters
                    // or estimate based on connections

                    // Since accurate per-process network monitoring requires kernel-level access,
                    // we'll use a simplified approach with IPv4 statistics

                    var currentTime = DateTime.Now;
                    long downloadBytes = 0;
                    long uploadBytes = 0;
                    double downloadKBps = 0;
                    double uploadKBps = 0;

                    // This is a simplified implementation
                    // For production, consider using ETW (Event Tracing for Windows) or WinPcap

                    if (_lastCheck.ContainsKey(pid))
                    {
                        var timeDiffSeconds = (currentTime - _lastCheck[pid]).TotalSeconds;

                        if (timeDiffSeconds > 0)
                        {
                            if (_lastBytesReceived.ContainsKey(pid) && _lastBytesSent.ContainsKey(pid))
                            {
                                downloadKBps = (_lastBytesReceived[pid]) / timeDiffSeconds / 1024.0;
                                uploadKBps = (_lastBytesSent[pid]) / timeDiffSeconds / 1024.0;
                            }
                        }
                    }

                    _lastCheck[pid] = currentTime;

                    // Store simulated values (in real implementation, get actual network I/O)
                    _lastBytesReceived[pid] = downloadBytes;
                    _lastBytesSent[pid] = uploadBytes;

                    return (downloadBytes, uploadBytes, downloadKBps, uploadKBps);
                }
                catch
                {
                    return (0, 0, 0, 0);
                }
            }
        }

        public (double downloadKBps, double uploadKBps) GetSystemNetworkStats()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                    return (0, 0);

                long totalReceived = 0;
                long totalSent = 0;

                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var stats = nic.GetIPv4Statistics();
                        totalReceived += stats.BytesReceived;
                        totalSent += stats.BytesSent;
                    }
                }

                var currentTime = DateTime.Now;
                var timeDiff = (currentTime - _lastTotalCheck).TotalSeconds;

                double downloadKBps = 0;
                double uploadKBps = 0;

                if (timeDiff > 0 && _lastTotalBytesReceived > 0)
                {
                    downloadKBps = (totalReceived - _lastTotalBytesReceived) / timeDiff / 1024.0;
                    uploadKBps = (totalSent - _lastTotalBytesSent) / timeDiff / 1024.0;
                }

                _lastTotalBytesReceived = totalReceived;
                _lastTotalBytesSent = totalSent;
                _lastTotalCheck = currentTime;

                return (Math.Max(0, downloadKBps), Math.Max(0, uploadKBps));
            }
            catch
            {
                return (0, 0);
            }
        }

        public void CleanupProcess(int pid)
        {
            lock (_lock)
            {
                _lastBytesReceived.Remove(pid);
                _lastBytesSent.Remove(pid);
                _lastCheck.Remove(pid);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _lastBytesReceived.Clear();
                _lastBytesSent.Clear();
                _lastCheck.Clear();
            }
        }
    }
}
