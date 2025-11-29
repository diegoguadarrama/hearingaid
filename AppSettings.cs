using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace HearingAI
{
    public class AppSettings
    {
        public float VolumeThreshold { get; set; } = 0.1f;
        public int FlashDuration { get; set; } = 200;
        public string LeftFlashColorHex { get; set; } = "#FF0000"; // Red
        public string RightFlashColorHex { get; set; } = "#FF0000"; // Red
        public float FlashOpacity { get; set; } = 0.8f; // 80% opacity
        public bool MinimizeToTray { get; set; } = true;
        public bool LeftChannelEnabled { get; set; } = true;
        public bool RightChannelEnabled { get; set; } = true;
        public string SelectedAudioDeviceId { get; set; } = ""; // Empty string means default device
        public string SelectedMonitorId { get; set; } = ""; // Empty string means primary monitor
        public int SelectedMonitorIndex { get; set; } = 0; // Monitor index for compatibility
        public int ChannelTriggerMode { get; set; } = 0; // 0=Independent, 1=Exclusive, 2=Threshold
        public float ChannelSeparationThreshold { get; set; } = 0.02f;
        
        // Enhanced detection settings
        public float AudioGain { get; set; } = 1.0f; // Audio amplification multiplier
        public bool UseAdaptiveThreshold { get; set; } = false; // Automatically adjust threshold
        public float NoiseFloor { get; set; } = 0.001f; // Minimum noise level
        public int SensitivityMode { get; set; } = 0; // 0=Normal, 1=High, 2=Ultra-High
        
        // Frequency analysis settings
        public bool EnableFrequencyAnalysis { get; set; } = false;
        public bool EnableEventDetection { get; set; } = false;
        public float EventDetectionSensitivity { get; set; } = 0.7f;

        [System.Text.Json.Serialization.JsonIgnore]
        public Color LeftFlashColor
        {
            get => ColorTranslator.FromHtml(LeftFlashColorHex);
            set => LeftFlashColorHex = ColorTranslator.ToHtml(value);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public Color RightFlashColor
        {
            get => ColorTranslator.FromHtml(RightFlashColorHex);
            set => RightFlashColorHex = ColorTranslator.ToHtml(value);
        }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HearingAI",
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                // If loading fails, return default settings
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception)
            {
                // Ignore save errors
            }
        }
    }
}
