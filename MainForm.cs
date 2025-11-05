using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScottPlot;

namespace PerformanceWatcher
{
    public partial class MainForm : Form
    {
        private MetricsCollector? _metricsCollector;
        private ProcessMonitor? _processMonitor;
        private readonly Dictionary<string, System.Drawing.Color> _processColors = new();
        private readonly Random _random = new();
        private DateTime _startTime;
        private int _elapsedSeconds = 0;

        public MainForm()
        {
            InitializeComponent();
            InitializeApplication();
            SetupEventHandlers();
            SetupDataGridView();
            InitializeChart();
        }

        private void InitializeApplication()
        {
            _metricsCollector = new MetricsCollector();
            _processMonitor = new ProcessMonitor();
            _startTime = DateTime.Now;

            // Start the refresh timer
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void InitializeChart()
        {
            performanceChart.Plot.Title("Process Performance Monitor");
            performanceChart.Plot.XLabel("Time (seconds)");
            performanceChart.Plot.YLabel("Value");

            // Configure grid
            performanceChart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#e0e0e0");
            performanceChart.Plot.Grid.IsVisible = true;

            performanceChart.Refresh();
        }

        private void SetupEventHandlers()
        {
            searchButton.Click += SearchButton_Click;
            searchTextBox.KeyPress += SearchTextBox_KeyPress;
            addToMonitorButton.Click += AddToMonitorButton_Click;
            removeProcessButton.Click += RemoveProcessButton_Click;
            searchResultsListBox.DoubleClick += (s, e) => AddToMonitorButton_Click(s, e);
            metricTypeComboBox.SelectedIndexChanged += ChartSettings_Changed;
            viewModeComboBox.SelectedIndexChanged += ChartSettings_Changed;
            maxTimeSecondsNumeric.ValueChanged += ChartSettings_Changed;
            maxValueNumeric.ValueChanged += ChartSettings_Changed;
            autoScaleCheckBox.CheckedChanged += ChartSettings_Changed;
            this.FormClosing += MainForm_FormClosing;
        }

        private void SetupDataGridView()
        {
            metricsDataGridView.Columns.Clear();
            metricsDataGridView.Columns.Add("ProcessName", "Process");
            metricsDataGridView.Columns.Add("PIDs", "PIDs");
            metricsDataGridView.Columns.Add("CPU", "CPU %");
            metricsDataGridView.Columns.Add("Memory", "Memory (MB)");
            metricsDataGridView.Columns.Add("NetDown", "Download (KB/s)");
            metricsDataGridView.Columns.Add("NetUp", "Upload (KB/s)");

            metricsDataGridView.Columns["ProcessName"].Width = 120;
            metricsDataGridView.Columns["PIDs"].Width = 80;
            metricsDataGridView.Columns["CPU"].Width = 80;
            metricsDataGridView.Columns["Memory"].Width = 110;
            metricsDataGridView.Columns["NetDown"].Width = 130;
            metricsDataGridView.Columns["NetUp"].Width = 120;
        }

        private void SearchTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                SearchButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            try
            {
                searchResultsListBox.Items.Clear();
                var searchTerm = searchTextBox.Text.Trim();

                if (string.IsNullOrEmpty(searchTerm))
                {
                    statusLabel.Text = "Please enter a search term";
                    return;
                }

                var processes = _processMonitor?.SearchProcessesByName(searchTerm);

                if (processes == null || processes.Count == 0)
                {
                    statusLabel.Text = $"No processes found matching '{searchTerm}'";
                    return;
                }

                foreach (var process in processes)
                {
                    try
                    {
                        searchResultsListBox.Items.Add(
                            $"{process.ProcessName} (PID: {process.Id})");
                    }
                    catch
                    {
                        // Process may have exited
                    }
                }

                statusLabel.Text = $"Found {searchResultsListBox.Items.Count} process(es)";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void AddToMonitorButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (searchResultsListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a process to monitor", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedItems = searchResultsListBox.SelectedItems.Cast<string>().ToList();

                foreach (var item in selectedItems)
                {
                    // Parse process name and PID from "ProcessName (PID: 1234)"
                    var parts = item.Split(new[] { " (PID: " }, StringSplitOptions.None);
                    if (parts.Length != 2) continue;

                    var processName = parts[0];
                    var pidStr = parts[1].TrimEnd(')');

                    if (int.TryParse(pidStr, out int pid))
                    {
                        _metricsCollector?.AddProcess(processName, new List<int> { pid });

                        if (!_processColors.ContainsKey(processName))
                        {
                            _processColors[processName] = GenerateRandomColor();
                        }
                    }
                }

                UpdateMonitoredProcessesList();
                statusLabel.Text = $"Added {selectedItems.Count} process(es) to monitor";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding process: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveProcessButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (monitoredProcessesListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a process to remove", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedItems = monitoredProcessesListBox.SelectedItems
                    .Cast<string>()
                    .Select(s => s.Split('[')[0].Trim())
                    .ToList();

                foreach (var processName in selectedItems)
                {
                    _metricsCollector?.RemoveProcess(processName);
                    _processColors.Remove(processName);
                }

                UpdateMonitoredProcessesList();
                UpdateChart();
                statusLabel.Text = $"Removed {selectedItems.Count} process(es)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing process: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _elapsedSeconds++;

                // Collect metrics
                var metrics = _metricsCollector?.CollectMetrics();

                if (metrics != null)
                {
                    UpdateMetricsGrid(metrics);
                    UpdateChart();
                    UpdateMonitoredProcessesList();
                }

                var elapsed = DateTime.Now - _startTime;
                statusLabel.Text = $"Running | Elapsed: {elapsed:hh\\:mm\\:ss} | Monitoring: {metrics?.Count ?? 0} process(es)";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateMetricsGrid(List<ProcessMetrics> metrics)
        {
            metricsDataGridView.Rows.Clear();

            foreach (var metric in metrics.OrderByDescending(m => m.CpuPercent))
            {
                metricsDataGridView.Rows.Add(
                    metric.ProcessName,
                    string.Join(", ", metric.PIDs),
                    $"{metric.CpuPercent:F1}",
                    $"{metric.MemoryMB:F1}",
                    $"{metric.NetworkDownloadKBps:F2}",
                    $"{metric.NetworkUploadKBps:F2}"
                );
            }
        }

        private void UpdateMonitoredProcessesList()
        {
            var currentSelection = monitoredProcessesListBox.SelectedItem?.ToString();
            monitoredProcessesListBox.Items.Clear();

            var processNames = _metricsCollector?.GetMonitoredProcessNames() ?? new List<string>();

            foreach (var processName in processNames)
            {
                var monitoredProcess = _metricsCollector?.GetMonitoredProcess(processName);
                if (monitoredProcess != null)
                {
                    var pidCount = monitoredProcess.PIDs.Count;
                    monitoredProcessesListBox.Items.Add(
                        $"{processName} [{pidCount} PID(s)]");
                }
            }

            // Restore selection if possible
            if (currentSelection != null)
            {
                var index = monitoredProcessesListBox.Items.IndexOf(currentSelection);
                if (index >= 0)
                    monitoredProcessesListBox.SelectedIndex = index;
            }
        }

        private void UpdateChart()
        {
            try
            {
                performanceChart.Plot.Clear();

                var maxTimeSeconds = (int)maxTimeSecondsNumeric.Value;
                var isAggregated = viewModeComboBox.SelectedIndex == 1;
                var metricType = metricTypeComboBox.SelectedIndex;

                // Calculate time window
                int minSeconds = Math.Max(0, _elapsedSeconds - maxTimeSeconds);
                int maxSeconds = _elapsedSeconds;

                if (isAggregated)
                {
                    // Aggregated view
                    var aggregatedData = _metricsCollector?.GetAggregatedHistoricalData(maxTimeSeconds);
                    if (aggregatedData != null && aggregatedData.Count > 0)
                    {
                        var xData = new List<double>();
                        var yData = new List<double>();

                        int dataIndex = 0;
                        foreach (var data in aggregatedData)
                        {
                            var value = GetMetricValue(data, metricType);
                            var timeOffset = minSeconds + dataIndex;
                            xData.Add(timeOffset);
                            yData.Add(value);
                            dataIndex++;
                        }

                        if (xData.Count > 0)
                        {
                            var scatter = performanceChart.Plot.Add.Scatter(xData, yData);
                            scatter.LineWidth = 2;
                            scatter.Color = ScottPlot.Color.FromColor(System.Drawing.Color.FromArgb(0, 120, 215));
                            scatter.LegendText = "Total";
                        }
                    }
                }
                else
                {
                    // Per-process view
                    var processNames = _metricsCollector?.GetMonitoredProcessNames() ?? new List<string>();

                    foreach (var processName in processNames)
                    {
                        var historicalData = _metricsCollector?.GetHistoricalData(processName, maxTimeSeconds);
                        if (historicalData == null || historicalData.Count == 0)
                            continue;

                        var xData = new List<double>();
                        var yData = new List<double>();

                        int dataIndex = 0;
                        foreach (var data in historicalData)
                        {
                            var value = GetMetricValue(data, metricType);
                            var timeOffset = minSeconds + dataIndex;
                            xData.Add(timeOffset);
                            yData.Add(value);
                            dataIndex++;
                        }

                        if (xData.Count > 0)
                        {
                            var scatter = performanceChart.Plot.Add.Scatter(xData, yData);
                            scatter.LineWidth = 2;

                            var color = _processColors.ContainsKey(processName)
                                ? _processColors[processName]
                                : GenerateRandomColor();
                            scatter.Color = ScottPlot.Color.FromColor(color);
                            scatter.LegendText = processName;
                        }
                    }
                }

                // Configure axes
                performanceChart.Plot.Axes.SetLimitsX(minSeconds, maxSeconds > minSeconds ? maxSeconds : minSeconds + 1);
                performanceChart.Plot.XLabel($"Time (seconds) - Window: {minSeconds} to {maxSeconds}");

                // Y-axis configuration
                var metricNames = new[] { "CPU %", "Memory (MB)", "Network Download (KB/s)", "Network Upload (KB/s)" };
                performanceChart.Plot.YLabel(metricNames[metricType]);

                if (autoScaleCheckBox.Checked)
                {
                    performanceChart.Plot.Axes.AutoScale();
                }
                else
                {
                    performanceChart.Plot.Axes.SetLimitsY(0, (double)maxValueNumeric.Value);
                }

                performanceChart.Plot.ShowLegend();
                performanceChart.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chart update error: {ex.Message}");
            }
        }

        private double GetMetricValue(ProcessMetrics metrics, int metricType)
        {
            return metricType switch
            {
                0 => metrics.CpuPercent,
                1 => metrics.MemoryMB,
                2 => metrics.NetworkDownloadKBps,
                3 => metrics.NetworkUploadKBps,
                _ => 0
            };
        }

        private void ChartSettings_Changed(object? sender, EventArgs e)
        {
            if (autoScaleCheckBox.Checked)
            {
                maxValueNumeric.Enabled = false;
            }
            else
            {
                maxValueNumeric.Enabled = true;
            }

            UpdateChart();
        }

        private System.Drawing.Color GenerateRandomColor()
        {
            // Generate vibrant colors
            var colors = new[]
            {
                System.Drawing.Color.FromArgb(0, 120, 215),   // Blue
                System.Drawing.Color.FromArgb(16, 137, 62),   // Green
                System.Drawing.Color.FromArgb(232, 17, 35),   // Red
                System.Drawing.Color.FromArgb(255, 140, 0),   // Orange
                System.Drawing.Color.FromArgb(138, 43, 226),  // Purple
                System.Drawing.Color.FromArgb(0, 153, 188),   // Cyan
                System.Drawing.Color.FromArgb(255, 20, 147),  // Pink
                System.Drawing.Color.FromArgb(255, 215, 0),   // Gold
                System.Drawing.Color.FromArgb(0, 128, 128),   // Teal
                System.Drawing.Color.FromArgb(218, 112, 214)  // Orchid
            };

            return colors[_random.Next(colors.Length)];
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop();
            _metricsCollector?.Dispose();
            _processMonitor?.Dispose();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Handle responsive layout
            if (topPanel != null && this.ClientSize.Width < 1200)
            {
                // Adjust for smaller screens
                if (searchPanel != null)
                    searchPanel.Width = Math.Max(300, (this.ClientSize.Width - 60) / 2);

                if (monitorPanel != null && searchPanel != null)
                {
                    monitorPanel.Left = searchPanel.Right + 10;
                    monitorPanel.Width = searchPanel.Width;
                }
            }
        }
    }
}
