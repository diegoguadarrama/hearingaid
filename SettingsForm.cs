using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace HearingAI
{
    public partial class SettingsForm : Form
    {
        private readonly AudioAnalyzer audioAnalyzer;
        private readonly OverlayWindow overlayWindow;
        private readonly NotifyIcon notifyIcon;
        private readonly AppSettings settings;

        // UI Controls
        private TrackBar volumeThresholdTrackBar = null!;
        private Label volumeThresholdLabel = null!;
        private NumericUpDown flashDurationNumeric = null!;
        private Button leftColorButton = null!;
        private Button rightColorButton = null!;
        private TrackBar opacityTrackBar = null!;
        private Label opacityLabel = null!;
        private CheckBox leftChannelCheckBox = null!;
        private CheckBox rightChannelCheckBox = null!;
        private ComboBox channelTriggerModeComboBox = null!;
        private TrackBar separationThresholdTrackBar = null!;
        private Label separationThresholdLabel = null!;
        private TrackBar audioGainTrackBar = null!;
        private Label audioGainLabel = null!;
        private ComboBox sensitivityModeComboBox = null!;
        private CheckBox adaptiveThresholdCheckBox = null!;
        private CheckBox enableFrequencyAnalysisCheckBox = null!;
        private CheckBox enableEventDetectionCheckBox = null!;
        private TrackBar eventSensitivityTrackBar = null!;
        private Label eventSensitivityLabel = null!;
        private ListBox eventLogListBox = null!;
        private ComboBox audioDeviceComboBox = null!;
        private Button refreshDevicesButton = null!;
        private Label currentDeviceLabel = null!;
        private ComboBox monitorComboBox = null!;
        private Button refreshMonitorsButton = null!;
        private Button startButton = null!;
        private Button stopButton = null!;
        private Label statusLabel = null!;
        private CheckBox minimizeToTrayCheckBox = null!;

        public SettingsForm()
        {
            settings = AppSettings.Load();
            
            // Initialize components first
            audioAnalyzer = new AudioAnalyzer();
            overlayWindow = new OverlayWindow();
            notifyIcon = new NotifyIcon();
            
            InitializeComponent();
            InitializeNotifyIcon();
            LoadSettings();
            LoadAudioDevices();
            LoadMonitors();

            // Apply persisted settings to analyzer and overlay
            try
            {
                // AudioAnalyzer configuration
                audioAnalyzer.VolumeThreshold = settings.VolumeThreshold;
                audioAnalyzer.LeftChannelEnabled = settings.LeftChannelEnabled;
                audioAnalyzer.RightChannelEnabled = settings.RightChannelEnabled;
                audioAnalyzer.ChannelTriggerMode = (ChannelTriggerMode)settings.ChannelTriggerMode;
                audioAnalyzer.ChannelSeparationThreshold = settings.ChannelSeparationThreshold;
                audioAnalyzer.AudioGain = settings.AudioGain;
                audioAnalyzer.UseAdaptiveThreshold = settings.UseAdaptiveThreshold;
                audioAnalyzer.SensitivityMode = settings.SensitivityMode;
                audioAnalyzer.EnableFrequencyAnalysis = settings.EnableFrequencyAnalysis;
                audioAnalyzer.EnableEventDetection = settings.EnableEventDetection;
                audioAnalyzer.EventDetectionSensitivity = settings.EventDetectionSensitivity;
                audioAnalyzer.SelectedDeviceId = string.IsNullOrWhiteSpace(settings.SelectedAudioDeviceId) ? null : settings.SelectedAudioDeviceId;

                // Overlay configuration
                overlayWindow.LeftFlashColor = settings.LeftFlashColor;
                overlayWindow.RightFlashColor = settings.RightFlashColor;
                overlayWindow.FlashDuration = settings.FlashDuration;
                overlayWindow.FlashOpacity = settings.FlashOpacity;
                var screen = GetSelectedScreen();
                if (screen != null)
                {
                    overlayWindow.UpdateTargetScreen(screen);
                }
                // Show overlay so flashes are visible
                overlayWindow.Show();

                // Event wiring
                audioAnalyzer.LeftChannelActive += (s, e) => overlayWindow.FlashLeft();
                audioAnalyzer.RightChannelActive += (s, e) => overlayWindow.FlashRight();
                audioAnalyzer.AudioEventDetected += (s, det) => OnAudioEventDetected(det.EventType.ToString(), det.Confidence);
                audioAnalyzer.DeviceChanged += (s, msg) =>
                {
                    if (currentDeviceLabel == null) return;
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => { currentDeviceLabel.Text = msg; currentDeviceLabel.ForeColor = Color.Green; }));
                    }
                    else
                    {
                        currentDeviceLabel.Text = msg;
                        currentDeviceLabel.ForeColor = Color.Green;
                    }
                };
            }
            catch
            {
                // Non-fatal: keep UI usable even if wiring fails
            }
            
            // Ensure the form is in a normal state on startup (Application.Run handles showing)
            this.WindowState = FormWindowState.Normal;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void InitializeComponent()
        {
            this.Text = "HearingAI - Audio Visual Alert Settings";
            this.Size = new Size(1200, 700);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Information;

            // Main horizontal layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(10)
            };
            
            // Set column styles for equal distribution
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            
            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 85f)); // Main content
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15f)); // Controls and status

            // Create the three main groups
            var basicSettingsGroup = CreateBasicSettingsGroup();
            var advancedDetectionGroup = CreateAdvancedDetectionGroup();
            var deviceAndFrequencyGroup = CreateDeviceAndFrequencyGroup();
            var controlsAndStatusGroup = CreateControlsAndStatusGroup();

            // Add groups to main panel
            mainPanel.Controls.Add(basicSettingsGroup, 0, 0);
            mainPanel.Controls.Add(advancedDetectionGroup, 1, 0);
            mainPanel.Controls.Add(deviceAndFrequencyGroup, 2, 0);
            mainPanel.SetColumnSpan(controlsAndStatusGroup, 3);
            mainPanel.Controls.Add(controlsAndStatusGroup, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private GroupBox CreateBasicSettingsGroup()
        {
            var groupBox = new GroupBox
            {
                Text = "Basic Audio Settings",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10
            };
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Volume threshold controls
            var volumeLabel = new Label
            {
                Text = "Volume Threshold:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int vtVal = (int)(settings.VolumeThreshold * 1000);
            vtVal = ClampInt(vtVal, 1, 100);
            volumeThresholdTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = 100,
                Value = vtVal,
                TickFrequency = 10,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            volumeThresholdTrackBar.ValueChanged += VolumeThresholdTrackBar_ValueChanged;

            volumeThresholdLabel = new Label
            {
                Text = $"{settings.VolumeThreshold:F3}",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            // Flash duration
            var durationLabel = new Label
            {
                Text = "Flash Duration (ms):",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int fdVal = ClampInt(settings.FlashDuration, 100, 5000);
            flashDurationNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 5000,
                Value = fdVal,
                Increment = 100,
                Width = 100,
                Anchor = AnchorStyles.Left
            };
            flashDurationNumeric.ValueChanged += FlashDurationNumeric_ValueChanged;

            // Color settings
            var leftColorLabel = new Label
            {
                Text = "Left Channel Color:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            leftColorButton = new Button
            {
                BackColor = settings.LeftFlashColor,
                Width = 100,
                Height = 30,
                Anchor = AnchorStyles.Left
            };
            leftColorButton.Click += LeftColorButton_Click;

            var rightColorLabel = new Label
            {
                Text = "Right Channel Color:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            rightColorButton = new Button
            {
                BackColor = settings.RightFlashColor,
                Width = 100,
                Height = 30,
                Anchor = AnchorStyles.Left
            };
            rightColorButton.Click += RightColorButton_Click;

            // Opacity controls
            var opacityLabel_text = new Label
            {
                Text = "Flash Opacity:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int opVal = (int)(settings.FlashOpacity * 100);
            opVal = ClampInt(opVal, 10, 100);
            opacityTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 100,
                Value = opVal,
                TickFrequency = 10,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            opacityTrackBar.ValueChanged += OpacityTrackBar_ValueChanged;

            opacityLabel = new Label
            {
                Text = $"{settings.FlashOpacity:P0}",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            // Channel controls
            var channelControlLabel = new Label
            {
                Text = "Channel Control:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var channelPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 25,
                AutoSize = true
            };

            leftChannelCheckBox = new CheckBox
            {
                Text = "Left",
                Checked = settings.LeftChannelEnabled,
                AutoSize = true
            };
            leftChannelCheckBox.CheckedChanged += LeftChannelCheckBox_CheckedChanged;

            rightChannelCheckBox = new CheckBox
            {
                Text = "Right",
                Checked = settings.RightChannelEnabled,
                AutoSize = true
            };
            rightChannelCheckBox.CheckedChanged += RightChannelCheckBox_CheckedChanged;

            channelPanel.Controls.AddRange(new Control[] { leftChannelCheckBox, rightChannelCheckBox });

            // Add controls to panel
            panel.Controls.Add(volumeLabel, 0, 0);
            panel.Controls.Add(volumeThresholdTrackBar, 1, 0);
            panel.Controls.Add(new Label(), 0, 1);
            panel.Controls.Add(volumeThresholdLabel, 1, 1);
            panel.Controls.Add(durationLabel, 0, 2);
            panel.Controls.Add(flashDurationNumeric, 1, 2);
            panel.Controls.Add(leftColorLabel, 0, 3);
            panel.Controls.Add(leftColorButton, 1, 3);
            panel.Controls.Add(rightColorLabel, 0, 4);
            panel.Controls.Add(rightColorButton, 1, 4);
            panel.Controls.Add(opacityLabel_text, 0, 5);
            panel.Controls.Add(opacityTrackBar, 1, 5);
            panel.Controls.Add(new Label(), 0, 6);
            panel.Controls.Add(opacityLabel, 1, 6);
            panel.Controls.Add(channelControlLabel, 0, 7);
            panel.Controls.Add(channelPanel, 1, 7);
            
            groupBox.Controls.Add(panel);
            return groupBox;
        }

        private GroupBox CreateAdvancedDetectionGroup()
        {
            var groupBox = new GroupBox
            {
                Text = "Advanced Detection",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8
            };
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Audio Gain
            var audioGainTextLabel = new Label
            {
                Text = "Audio Gain:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int agVal = (int)(settings.AudioGain * 100);
            agVal = ClampInt(agVal, 10, 500);
            audioGainTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 500,
                Value = agVal,
                TickFrequency = 50,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            audioGainTrackBar.ValueChanged += AudioGainTrackBar_ValueChanged;

            audioGainLabel = new Label
            {
                Text = $"{settings.AudioGain:F1}x",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            // Sensitivity Mode
            var sensitivityTextLabel = new Label
            {
                Text = "Sensitivity Mode:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            sensitivityModeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            sensitivityModeComboBox.Items.AddRange(new[] { "Normal", "High", "Ultra-High" });
            sensitivityModeComboBox.SelectedIndex = ClampInt(settings.SensitivityMode, 0, 2);
            sensitivityModeComboBox.SelectedIndexChanged += SensitivityModeComboBox_SelectedIndexChanged;

            // Channel Trigger Mode
            var triggerModeLabel = new Label
            {
                Text = "Channel Trigger Mode:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            channelTriggerModeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            channelTriggerModeComboBox.Items.AddRange(new[] { "Independent", "Exclusive", "Threshold" });
            channelTriggerModeComboBox.SelectedIndex = ClampInt(settings.ChannelTriggerMode, 0, 2);
            channelTriggerModeComboBox.SelectedIndexChanged += ChannelTriggerModeComboBox_SelectedIndexChanged;

            // Channel Separation Threshold
            var separationLabel = new Label
            {
                Text = "Channel Separation:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int sepVal = (int)(settings.ChannelSeparationThreshold * 1000);
            sepVal = ClampInt(sepVal, 1, 20);
            separationThresholdTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = 20,
                Value = sepVal,
                TickFrequency = 5,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            separationThresholdTrackBar.ValueChanged += SeparationThresholdTrackBar_ValueChanged;

            separationThresholdLabel = new Label
            {
                Text = $"{settings.ChannelSeparationThreshold:F3}",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            // Adaptive Threshold
            var adaptivePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 25,
                AutoSize = true
            };

            adaptiveThresholdCheckBox = new CheckBox
            {
                Text = "Use Adaptive Threshold",
                Checked = settings.UseAdaptiveThreshold,
                AutoSize = true
            };
            adaptiveThresholdCheckBox.CheckedChanged += AdaptiveThresholdCheckBox_CheckedChanged;

            adaptivePanel.Controls.Add(adaptiveThresholdCheckBox);

            // Add controls to panel
            panel.Controls.Add(audioGainTextLabel, 0, 0);
            panel.Controls.Add(audioGainTrackBar, 1, 0);
            panel.Controls.Add(new Label(), 0, 1);
            panel.Controls.Add(audioGainLabel, 1, 1);
            panel.Controls.Add(sensitivityTextLabel, 0, 2);
            panel.Controls.Add(sensitivityModeComboBox, 1, 2);
            panel.Controls.Add(triggerModeLabel, 0, 3);
            panel.Controls.Add(channelTriggerModeComboBox, 1, 3);
            panel.Controls.Add(separationLabel, 0, 4);
            panel.Controls.Add(separationThresholdTrackBar, 1, 4);
            panel.Controls.Add(new Label(), 0, 5);
            panel.Controls.Add(separationThresholdLabel, 1, 5);
            panel.Controls.Add(new Label(), 0, 6);
            panel.Controls.Add(adaptivePanel, 1, 6);
            
            groupBox.Controls.Add(panel);
            return groupBox;
        }

        private GroupBox CreateDeviceAndFrequencyGroup()
        {
            var groupBox = new GroupBox
            {
                Text = "Device & Frequency Analysis",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10
            };
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Audio Device Selection
            var audioDeviceLabel = new Label
            {
                Text = "Audio Device:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var devicePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 60,
                AutoSize = true
            };

            audioDeviceComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            audioDeviceComboBox.SelectedIndexChanged += AudioDeviceComboBox_SelectedIndexChanged;

            refreshDevicesButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 25),
                BackColor = Color.LightGray
            };
            refreshDevicesButton.Click += RefreshDevicesButton_Click;

            currentDeviceLabel = new Label
            {
                Text = "Current: Not monitoring",
                AutoSize = true,
                ForeColor = Color.Blue
            };

            devicePanel.Controls.AddRange(new Control[] { audioDeviceComboBox, refreshDevicesButton, currentDeviceLabel });

            // Monitor Selection
            var monitorLabel = new Label
            {
                Text = "Display Monitor:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var monitorPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 30,
                AutoSize = true
            };

            monitorComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            monitorComboBox.SelectedIndexChanged += MonitorComboBox_SelectedIndexChanged;

            refreshMonitorsButton = new Button
            {
                Text = "Refresh",
                Size = new Size(80, 25),
                BackColor = Color.LightGray
            };
            refreshMonitorsButton.Click += RefreshMonitorsButton_Click;

            monitorPanel.Controls.AddRange(new Control[] { monitorComboBox, refreshMonitorsButton });

            // Frequency Analysis
            var frequencyPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 80,
                AutoSize = true
            };

            enableFrequencyAnalysisCheckBox = new CheckBox
            {
                Text = "Enable Frequency Analysis",
                Checked = settings.EnableFrequencyAnalysis,
                AutoSize = true
            };
            enableFrequencyAnalysisCheckBox.CheckedChanged += EnableFrequencyAnalysisCheckBox_CheckedChanged;

            enableEventDetectionCheckBox = new CheckBox
            {
                Text = "Enable Event Detection",
                Checked = settings.EnableEventDetection,
                AutoSize = true
            };
            enableEventDetectionCheckBox.CheckedChanged += EnableEventDetectionCheckBox_CheckedChanged;

            frequencyPanel.Controls.AddRange(new Control[] { enableFrequencyAnalysisCheckBox, enableEventDetectionCheckBox });

            // Event Detection Sensitivity
            var eventSensitivityTextLabel = new Label
            {
                Text = "Event Sensitivity:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            int evVal = (int)(settings.EventDetectionSensitivity * 100);
            evVal = ClampInt(evVal, 10, 100);
            eventSensitivityTrackBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 100,
                Value = evVal,
                TickFrequency = 10,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            eventSensitivityTrackBar.ValueChanged += EventSensitivityTrackBar_ValueChanged;

            eventSensitivityLabel = new Label
            {
                Text = $"{settings.EventDetectionSensitivity:P0}",
                Anchor = AnchorStyles.Left,
                AutoSize = true
            };

            // Event Log
            var eventLogLabel = new Label
            {
                Text = "Recent Events:",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft
            };

            eventLogListBox = new ListBox
            {
                Height = 60,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            // Add controls to panel
            panel.Controls.Add(audioDeviceLabel, 0, 0);
            panel.Controls.Add(devicePanel, 1, 0);
            panel.Controls.Add(monitorLabel, 0, 1);
            panel.Controls.Add(monitorPanel, 1, 1);
            panel.Controls.Add(new Label { Text = "Frequency Analysis:" }, 0, 2);
            panel.Controls.Add(frequencyPanel, 1, 2);
            panel.Controls.Add(eventSensitivityTextLabel, 0, 3);
            panel.Controls.Add(eventSensitivityTrackBar, 1, 3);
            panel.Controls.Add(new Label(), 0, 4);
            panel.Controls.Add(eventSensitivityLabel, 1, 4);
            panel.Controls.Add(eventLogLabel, 0, 5);
            panel.Controls.Add(eventLogListBox, 1, 5);
            
            groupBox.Controls.Add(panel);
            return groupBox;
        }

        private GroupBox CreateControlsAndStatusGroup()
        {
            var groupBox = new GroupBox
            {
                Text = "Controls & Status",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2
            };
            
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Control buttons
            startButton = new Button
            {
                Text = "Start Monitoring",
                Size = new Size(120, 30),
                BackColor = Color.LightGreen,
                Anchor = AnchorStyles.None
            };
            startButton.Click += StartButton_Click;

            stopButton = new Button
            {
                Text = "Stop Monitoring",
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false,
                Anchor = AnchorStyles.None
            };
            stopButton.Click += StopButton_Click;

            // Test buttons
            var testLeftButton = new Button
            {
                Text = "Test Left",
                Size = new Size(80, 30),
                BackColor = Color.LightBlue,
                Anchor = AnchorStyles.None
            };
            testLeftButton.Click += (s, e) => 
            { 
                if (settings.LeftChannelEnabled)
                    overlayWindow.FlashLeft();
                else
                    MessageBox.Show("Left channel detection is disabled.", "Channel Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var testRightButton = new Button
            {
                Text = "Test Right",
                Size = new Size(80, 30),
                BackColor = Color.LightBlue,
                Anchor = AnchorStyles.None
            };
            testRightButton.Click += (s, e) => 
            { 
                if (settings.RightChannelEnabled)
                    overlayWindow.FlashRight();
                else
                    MessageBox.Show("Right channel detection is disabled.", "Channel Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Status and options
            statusLabel = new Label
            {
                Text = "Status: Ready to start",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Blue
            };

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to tray",
                Checked = settings.MinimizeToTray,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            minimizeToTrayCheckBox.CheckedChanged += (s, e) => 
            { 
                settings.MinimizeToTray = minimizeToTrayCheckBox.Checked; 
                settings.Save(); 
            };

            // Add controls to panel
            panel.Controls.Add(startButton, 0, 0);
            panel.Controls.Add(stopButton, 1, 0);
            panel.Controls.Add(testLeftButton, 2, 0);
            panel.Controls.Add(testRightButton, 3, 0);
            panel.SetColumnSpan(statusLabel, 2);
            panel.Controls.Add(statusLabel, 0, 1);
            panel.SetColumnSpan(minimizeToTrayCheckBox, 2);
            panel.Controls.Add(minimizeToTrayCheckBox, 2, 1);
            
            groupBox.Controls.Add(panel);
            return groupBox;
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon.Icon = SystemIcons.Information;
            notifyIcon.Text = "HearingAI - Audio Visual Alert";
            notifyIcon.Visible = false; // Hidden initially
            notifyIcon.DoubleClick += (sender, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            };
        }

        private void LoadSettings()
        {
            // Load settings and update UI controls
            volumeThresholdTrackBar.Value = ClampInt((int)(settings.VolumeThreshold * 1000), volumeThresholdTrackBar.Minimum, volumeThresholdTrackBar.Maximum);
            flashDurationNumeric.Value = ClampInt(settings.FlashDuration, (int)flashDurationNumeric.Minimum, (int)flashDurationNumeric.Maximum);
            leftColorButton.BackColor = settings.LeftFlashColor;
            rightColorButton.BackColor = settings.RightFlashColor;
            opacityTrackBar.Value = ClampInt((int)(settings.FlashOpacity * 100), opacityTrackBar.Minimum, opacityTrackBar.Maximum);
            leftChannelCheckBox.Checked = settings.LeftChannelEnabled;
            rightChannelCheckBox.Checked = settings.RightChannelEnabled;
            channelTriggerModeComboBox.SelectedIndex = ClampInt(settings.ChannelTriggerMode, 0, channelTriggerModeComboBox.Items.Count - 1);
            separationThresholdTrackBar.Value = ClampInt((int)(settings.ChannelSeparationThreshold * 1000), separationThresholdTrackBar.Minimum, separationThresholdTrackBar.Maximum);
            audioGainTrackBar.Value = ClampInt((int)(settings.AudioGain * 100), audioGainTrackBar.Minimum, audioGainTrackBar.Maximum);
            sensitivityModeComboBox.SelectedIndex = ClampInt(settings.SensitivityMode, 0, sensitivityModeComboBox.Items.Count - 1);
            adaptiveThresholdCheckBox.Checked = settings.UseAdaptiveThreshold;
            enableFrequencyAnalysisCheckBox.Checked = settings.EnableFrequencyAnalysis;
            enableEventDetectionCheckBox.Checked = settings.EnableEventDetection;
            eventSensitivityTrackBar.Value = ClampInt((int)(settings.EventDetectionSensitivity * 100), eventSensitivityTrackBar.Minimum, eventSensitivityTrackBar.Maximum);
            minimizeToTrayCheckBox.Checked = settings.MinimizeToTray;

            // Update labels
            UpdateVolumeThresholdLabel();
            UpdateOpacityLabel();
            UpdateSeparationThresholdLabel();
            UpdateAudioGainLabel();
            UpdateEventSensitivityLabel();
        }

        private void VolumeThresholdTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            settings.VolumeThreshold = volumeThresholdTrackBar.Value / 1000.0f;
            UpdateVolumeThresholdLabel();
            
            if (audioAnalyzer != null)
                audioAnalyzer.VolumeThreshold = settings.VolumeThreshold;
                
            settings.Save();
        }

        private void UpdateVolumeThresholdLabel()
        {
            volumeThresholdLabel.Text = $"{settings.VolumeThreshold:F3}";
        }

        private void FlashDurationNumeric_ValueChanged(object? sender, EventArgs e)
        {
            settings.FlashDuration = (int)flashDurationNumeric.Value;
            
            if (overlayWindow != null)
                overlayWindow.FlashDuration = settings.FlashDuration;
                
            settings.Save();
        }

        private void LeftColorButton_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            colorDialog.Color = settings.LeftFlashColor;
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                settings.LeftFlashColor = colorDialog.Color;
                leftColorButton.BackColor = settings.LeftFlashColor;
                
                if (overlayWindow != null)
                    overlayWindow.LeftFlashColor = settings.LeftFlashColor;
                    
                settings.Save();
            }
        }

        private void RightColorButton_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            colorDialog.Color = settings.RightFlashColor;
            
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                settings.RightFlashColor = colorDialog.Color;
                rightColorButton.BackColor = settings.RightFlashColor;
                
                if (overlayWindow != null)
                    overlayWindow.RightFlashColor = settings.RightFlashColor;
                    
                settings.Save();
            }
        }

        private void OpacityTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            settings.FlashOpacity = opacityTrackBar.Value / 100.0f;
            UpdateOpacityLabel();
            
            if (overlayWindow != null)
                overlayWindow.FlashOpacity = settings.FlashOpacity;
                
            settings.Save();
        }

        private void UpdateOpacityLabel()
        {
            opacityLabel.Text = $"{settings.FlashOpacity:P0}";
        }

        private void LeftChannelCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            settings.LeftChannelEnabled = leftChannelCheckBox.Checked;
            
            if (audioAnalyzer != null)
                audioAnalyzer.LeftChannelEnabled = settings.LeftChannelEnabled;
                
            settings.Save();
        }

        private void RightChannelCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            settings.RightChannelEnabled = rightChannelCheckBox.Checked;
            
            if (audioAnalyzer != null)
                audioAnalyzer.RightChannelEnabled = settings.RightChannelEnabled;
                
            settings.Save();
        }

        private void ChannelTriggerModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            settings.ChannelTriggerMode = channelTriggerModeComboBox.SelectedIndex;
            
            if (audioAnalyzer != null)
                audioAnalyzer.ChannelTriggerMode = (ChannelTriggerMode)settings.ChannelTriggerMode;
                
            settings.Save();
        }

        private void SeparationThresholdTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            settings.ChannelSeparationThreshold = separationThresholdTrackBar.Value / 1000.0f;
            UpdateSeparationThresholdLabel();
            
            if (audioAnalyzer != null)
                audioAnalyzer.ChannelSeparationThreshold = settings.ChannelSeparationThreshold;
                
            settings.Save();
        }

        private void UpdateSeparationThresholdLabel()
        {
            separationThresholdLabel.Text = $"{settings.ChannelSeparationThreshold:F3}";
        }

        private void AudioGainTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            settings.AudioGain = audioGainTrackBar.Value / 100.0f;
            UpdateAudioGainLabel();
            
            if (audioAnalyzer != null)
                audioAnalyzer.AudioGain = settings.AudioGain;
                
            settings.Save();
        }

        private void UpdateAudioGainLabel()
        {
            audioGainLabel.Text = $"{settings.AudioGain:F1}x";
        }

        private void SensitivityModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            settings.SensitivityMode = sensitivityModeComboBox.SelectedIndex;
            
            if (audioAnalyzer != null)
                audioAnalyzer.SensitivityMode = settings.SensitivityMode;
                
            settings.Save();
        }

        private void AdaptiveThresholdCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            settings.UseAdaptiveThreshold = adaptiveThresholdCheckBox.Checked;
            
            if (audioAnalyzer != null)
                audioAnalyzer.UseAdaptiveThreshold = settings.UseAdaptiveThreshold;
                
            settings.Save();
        }

        private void EnableFrequencyAnalysisCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            settings.EnableFrequencyAnalysis = enableFrequencyAnalysisCheckBox.Checked;
            
            if (audioAnalyzer != null)
                audioAnalyzer.EnableFrequencyAnalysis = settings.EnableFrequencyAnalysis;
                
            // Enable/disable event detection based on frequency analysis
            enableEventDetectionCheckBox.Enabled = settings.EnableFrequencyAnalysis;
            if (!settings.EnableFrequencyAnalysis)
            {
                enableEventDetectionCheckBox.Checked = false;
                settings.EnableEventDetection = false;
                if (audioAnalyzer != null)
                    audioAnalyzer.EnableEventDetection = false;
            }
                
            settings.Save();
        }

        private void EnableEventDetectionCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            settings.EnableEventDetection = enableEventDetectionCheckBox.Checked;
            
            if (audioAnalyzer != null)
                audioAnalyzer.EnableEventDetection = settings.EnableEventDetection;
                
            settings.Save();
        }

        private void EventSensitivityTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            settings.EventDetectionSensitivity = eventSensitivityTrackBar.Value / 100.0f;
            UpdateEventSensitivityLabel();
            
            if (audioAnalyzer != null)
                audioAnalyzer.EventDetectionSensitivity = settings.EventDetectionSensitivity;
                
            settings.Save();
        }

        private void UpdateEventSensitivityLabel()
        {
            eventSensitivityLabel.Text = $"{settings.EventDetectionSensitivity:P0}";
        }

        private void AudioDeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (audioDeviceComboBox.SelectedItem is MMDevice device)
            {
                settings.SelectedAudioDeviceId = device.ID;
                settings.Save();
                UpdateCurrentDeviceLabel();
            }
        }

        private void RefreshDevicesButton_Click(object? sender, EventArgs e)
        {
            LoadAudioDevices();
        }

        private void LoadAudioDevices()
        {
            audioDeviceComboBox.Items.Clear();
            
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                foreach (var device in devices)
                {
                    audioDeviceComboBox.Items.Add(device);
                }
                
                // Select the previously selected device or default
                var selectedDevice = audioDeviceComboBox.Items.Cast<MMDevice>()
                    .FirstOrDefault(d => d.ID == settings.SelectedAudioDeviceId);
                    
                if (selectedDevice != null)
                {
                    audioDeviceComboBox.SelectedItem = selectedDevice;
                }
                else if (audioDeviceComboBox.Items.Count > 0)
                {
                    audioDeviceComboBox.SelectedIndex = 0;
                }
                
                UpdateCurrentDeviceLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading audio devices: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateCurrentDeviceLabel()
        {
            if (audioAnalyzer.CurrentDevice != null)
            {
                currentDeviceLabel.Text = $"Current: {audioAnalyzer.CurrentDevice.Name}";
                currentDeviceLabel.ForeColor = Color.Green;
            }
            else
            {
                currentDeviceLabel.Text = "Current: Not monitoring";
                currentDeviceLabel.ForeColor = Color.Blue;
            }
        }

        private void MonitorComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            settings.SelectedMonitorIndex = monitorComboBox.SelectedIndex;
            settings.Save();
            // Update overlay target screen immediately
            try
            {
                var screen = GetSelectedScreen();
                if (screen != null)
                {
                    overlayWindow.UpdateTargetScreen(screen);
                }
            }
            catch { }
        }

        private void RefreshMonitorsButton_Click(object? sender, EventArgs e)
        {
            LoadMonitors();
        }

        private void LoadMonitors()
        {
            monitorComboBox.Items.Clear();
            
            try
            {
                var screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    var displayName = screen.Primary ? $"Monitor {i + 1} (Primary)" : $"Monitor {i + 1}";
                    monitorComboBox.Items.Add(displayName);
                }
                
                // Select the previously selected monitor or primary
                if (settings.SelectedMonitorIndex >= 0 && settings.SelectedMonitorIndex < monitorComboBox.Items.Count)
                {
                    monitorComboBox.SelectedIndex = settings.SelectedMonitorIndex;
                }
                else
                {
                    // Select primary monitor by default
                    for (int i = 0; i < screens.Length; i++)
                    {
                        if (screens[i].Primary)
                        {
                            monitorComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading monitors: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (audioDeviceComboBox.SelectedItem is MMDevice device && audioAnalyzer != null)
                {
                    audioAnalyzer.StartMonitoring(device.ID);
                    startButton.Enabled = false;
                    stopButton.Enabled = true;
                    statusLabel.Text = "Status: Monitoring audio...";
                    statusLabel.ForeColor = Color.Green;
                    UpdateCurrentDeviceLabel();
                }
                else
                {
                    MessageBox.Show("Please select an audio device first.", "No Device Selected", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting monitoring: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopButton_Click(object? sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void StopMonitoring()
        {
            try
            {
                audioAnalyzer.StopMonitoring();
                startButton.Enabled = true;
                stopButton.Enabled = false;
                statusLabel.Text = "Status: Ready to start";
                statusLabel.ForeColor = Color.Blue;
                UpdateCurrentDeviceLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping monitoring: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void OnAudioEventDetected(string eventType, double confidence)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, double>(OnAudioEventDetected), eventType, confidence);
                return;
            }

            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {eventType} (Confidence: {confidence:P0})";
            eventLogListBox.Items.Insert(0, logEntry);
            
            // Keep only the last 10 entries
            while (eventLogListBox.Items.Count > 10)
            {
                eventLogListBox.Items.RemoveAt(eventLogListBox.Items.Count - 1);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                if (notifyIcon != null)
                    notifyIcon.Visible = true;
            }
            else
            {
                StopMonitoring();
                audioAnalyzer.Dispose();
                overlayWindow.Close();
                base.OnFormClosing(e);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value && notifyIcon != null)
                notifyIcon.Visible = false;
        }

        public Screen? GetSelectedScreen()
        {
            try
            {
                var screens = Screen.AllScreens;
                if (settings.SelectedMonitorIndex >= 0 && settings.SelectedMonitorIndex < screens.Length)
                {
                    return screens[settings.SelectedMonitorIndex];
                }
                return Screen.PrimaryScreen;
            }
            catch
            {
                return Screen.PrimaryScreen;
            }
        }
    }
}
