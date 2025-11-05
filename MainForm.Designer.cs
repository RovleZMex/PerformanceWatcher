using System.Drawing;
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace PerformanceWatcher
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private Panel topPanel;
        private Panel searchPanel;
        private TextBox searchTextBox;
        private Button searchButton;
        private ListBox searchResultsListBox;
        private Button addToMonitorButton;
        private Panel monitorPanel;
        private ListBox monitoredProcessesListBox;
        private Button removeProcessButton;
        private DataGridView metricsDataGridView;
        private Panel chartPanel;
        private FormsPlot performanceChart;
        private Panel chartControlsPanel;
        private ComboBox metricTypeComboBox;
        private ComboBox viewModeComboBox;
        private NumericUpDown maxTimeSecondsNumeric;
        private NumericUpDown maxValueNumeric;
        private CheckBox autoScaleCheckBox;
        private Label metricLabel;
        private Label viewModeLabel;
        private Label maxTimeLabel;
        private Label maxValueLabel;
        private System.Windows.Forms.Timer refreshTimer;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Form settings
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1600, 900);
            this.Text = "Performance Watcher - Process Monitor";
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.MinimumSize = new Size(1200, 700);

            // Top Panel
            this.topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // Search Panel
            this.searchPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(500, 280),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var searchLabel = new Label
            {
                Text = "Search Processes",
                Location = new Point(10, 10),
                Size = new Size(480, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            this.searchTextBox = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(380, 25),
                Font = new Font("Segoe UI", 10F)
            };

            this.searchButton = new Button
            {
                Text = "Search",
                Location = new Point(400, 43),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            this.searchButton.FlatAppearance.BorderSize = 0;

            this.searchResultsListBox = new ListBox
            {
                Location = new Point(10, 80),
                Size = new Size(470, 150),
                Font = new Font("Consolas", 9F)
            };

            this.addToMonitorButton = new Button
            {
                Text = "Add to Monitor",
                Location = new Point(10, 240),
                Size = new Size(470, 30),
                BackColor = Color.FromArgb(16, 137, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.addToMonitorButton.FlatAppearance.BorderSize = 0;

            this.searchPanel.Controls.AddRange(new Control[]
            {
                searchLabel, this.searchTextBox, this.searchButton,
                this.searchResultsListBox, this.addToMonitorButton
            });

            // Monitor Panel
            this.monitorPanel = new Panel
            {
                Location = new Point(520, 10),
                Size = new Size(500, 280),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var monitorLabel = new Label
            {
                Text = "Monitored Processes",
                Location = new Point(10, 10),
                Size = new Size(480, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            this.monitoredProcessesListBox = new ListBox
            {
                Location = new Point(10, 45),
                Size = new Size(470, 185),
                Font = new Font("Consolas", 9F)
            };

            this.removeProcessButton = new Button
            {
                Text = "Remove Selected",
                Location = new Point(10, 240),
                Size = new Size(470, 30),
                BackColor = Color.FromArgb(232, 17, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.removeProcessButton.FlatAppearance.BorderSize = 0;

            this.monitorPanel.Controls.AddRange(new Control[]
            {
                monitorLabel, this.monitoredProcessesListBox, this.removeProcessButton
            });

            // Metrics DataGridView Panel
            var metricsPanel = new Panel
            {
                Location = new Point(1030, 10),
                Size = new Size(560, 280),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var metricsLabel = new Label
            {
                Text = "Current Metrics",
                Location = new Point(10, 10),
                Size = new Size(540, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            this.metricsDataGridView = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(540, 225),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Consolas", 8F)
            };

            metricsPanel.Controls.AddRange(new Control[] { metricsLabel, this.metricsDataGridView });

            this.topPanel.Controls.AddRange(new Control[]
            {
                this.searchPanel, this.monitorPanel, metricsPanel
            });

            // Chart Panel
            this.chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // Chart Controls Panel
            this.chartControlsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(10)
            };

            this.metricLabel = new Label
            {
                Text = "Metric:",
                Location = new Point(15, 15),
                Size = new Size(80, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            this.metricTypeComboBox = new ComboBox
            {
                Location = new Point(100, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            this.metricTypeComboBox.Items.AddRange(new object[]
            {
                "CPU %", "Memory (MB)", "Network Down (KB/s)", "Network Up (KB/s)"
            });
            this.metricTypeComboBox.SelectedIndex = 0;

            this.viewModeLabel = new Label
            {
                Text = "View:",
                Location = new Point(270, 15),
                Size = new Size(60, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            this.viewModeComboBox = new ComboBox
            {
                Location = new Point(335, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            this.viewModeComboBox.Items.AddRange(new object[] { "Per Process", "Aggregated" });
            this.viewModeComboBox.SelectedIndex = 0;

            this.maxTimeLabel = new Label
            {
                Text = "Max Time (s):",
                Location = new Point(505, 15),
                Size = new Size(100, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            this.maxTimeSecondsNumeric = new NumericUpDown
            {
                Location = new Point(610, 12),
                Size = new Size(100, 25),
                Minimum = 10,
                Maximum = 3600,
                Value = 300,
                Font = new Font("Segoe UI", 9F)
            };

            this.maxValueLabel = new Label
            {
                Text = "Max Y Value:",
                Location = new Point(730, 15),
                Size = new Size(100, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            this.maxValueNumeric = new NumericUpDown
            {
                Location = new Point(835, 12),
                Size = new Size(100, 25),
                Minimum = 0,
                Maximum = 100000,
                Value = 100,
                DecimalPlaces = 0,
                Font = new Font("Segoe UI", 9F)
            };

            this.autoScaleCheckBox = new CheckBox
            {
                Text = "Auto Scale Y",
                Location = new Point(955, 14),
                Size = new Size(120, 23),
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };

            this.chartControlsPanel.Controls.AddRange(new Control[]
            {
                this.metricLabel, this.metricTypeComboBox,
                this.viewModeLabel, this.viewModeComboBox,
                this.maxTimeLabel, this.maxTimeSecondsNumeric,
                this.maxValueLabel, this.maxValueNumeric,
                this.autoScaleCheckBox
            });

            // Performance Chart (ScottPlot)
            this.performanceChart = new FormsPlot
            {
                Dock = DockStyle.Fill
            };

            this.chartPanel.Controls.Add(this.performanceChart);
            this.chartPanel.Controls.Add(this.chartControlsPanel);

            // Status Strip
            this.statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(240, 240, 240)
            };

            this.statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 9F)
            };

            this.statusStrip.Items.Add(this.statusLabel);

            // Timer
            this.refreshTimer = new System.Windows.Forms.Timer(this.components)
            {
                Interval = 1000 // 1 second
            };

            // Add controls to form
            this.Controls.Add(this.chartPanel);
            this.Controls.Add(this.topPanel);
            this.Controls.Add(this.statusStrip);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
