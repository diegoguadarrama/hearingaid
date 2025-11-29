using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace HearingAI
{
    public enum ChannelTriggerMode
    {
        Independent, // Both channels can trigger simultaneously
        Exclusive,   // Only one channel triggers (the louder one)
        Threshold    // Only channels significantly above others trigger
    }

    public enum AudioEventType
    {
        Unknown = 0,
        Footsteps = 1,
        Gunshot = 2,
        Explosion = 3,
        VoiceShout = 4,
        Metallic = 5,
        Glass = 6,
        Impact = 7
    }

    public class AudioEventProfile
    {
        public AudioEventType EventType { get; set; }
        public string Name { get; set; } = "";
        public float MinFrequency { get; set; }
        public float MaxFrequency { get; set; }
        public float PeakFrequency { get; set; }
        public float MinIntensity { get; set; }
        public float Duration { get; set; } // Expected duration in ms
        public bool RequiresTransient { get; set; } // Quick attack/decay
        public float SpectralCentroid { get; set; } // Frequency "center of mass"
    }

    public class AudioEventDetection
    {
        public AudioEventType EventType { get; set; }
        public float Confidence { get; set; }
        public float Frequency { get; set; }
        public float Intensity { get; set; }
        public bool IsLeftChannel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AudioAnalyzer : IDisposable
    {
        private WasapiLoopbackCapture? capture;
        private volatile bool isRunning;
        private MMDevice? selectedDevice;
        
        public float VolumeThreshold { get; set; } = 0.1f;
        public bool LeftChannelEnabled { get; set; } = true;
        public bool RightChannelEnabled { get; set; } = true;
        public string? SelectedDeviceId { get; set; } // null means use default device
        public bool IsMonitoring => isRunning;
        public ChannelTriggerMode TriggerMode { get; set; } = ChannelTriggerMode.Independent;
        public ChannelTriggerMode ChannelTriggerMode 
        { 
            get => TriggerMode; 
            set => TriggerMode = value; 
        }
        public AudioDevice? CurrentDevice 
        { 
            get 
            {
                if (selectedDevice != null)
                {
                    return new AudioDevice 
                    { 
                        Id = selectedDevice.ID, 
                        Name = selectedDevice.FriendlyName, 
                        IsDefault = false 
                    };
                }
                return null;
            }
        }
        public float ChannelSeparationThreshold { get; set; } = 0.02f; // Minimum difference for exclusive mode
        
        // Enhanced detection properties
        public float AudioGain { get; set; } = 1.0f; // Audio amplification multiplier
        public bool UseAdaptiveThreshold { get; set; } = false; // Automatically adjust threshold based on ambient noise
        public float NoiseFloor { get; set; } = 0.001f; // Minimum noise level
        public int SensitivityMode { get; set; } = 0; // 0=Normal, 1=High, 2=Ultra-High
        
        // Adaptive threshold variables
        private float currentNoiseFloor = 0.001f;
        private float[] noiseHistory = new float[100]; // Keep last 100 noise samples
        private int noiseHistoryIndex = 0;
        
        // Frequency analysis properties
        public bool EnableFrequencyAnalysis { get; set; } = false;
        public bool EnableEventDetection { get; set; } = false;
        public float EventDetectionSensitivity { get; set; } = 0.7f; // 0.0 to 1.0
        
        // FFT variables
        private const int FFT_SIZE = 1024; // Must be power of 2
        private float[] fftBuffer = new float[FFT_SIZE];
        private float[] fftWindow = new float[FFT_SIZE];
        private float[] frequencyBins = new float[FFT_SIZE / 2];
        private int fftBufferIndex = 0;
        
        // Audio event profiles
        private readonly List<AudioEventProfile> eventProfiles = new List<AudioEventProfile>();
        
        // Events for channel activity
        public event EventHandler? LeftChannelActive;
        public event EventHandler? RightChannelActive;
        public event EventHandler<string>? DeviceChanged; // Event when audio device changes
        public event EventHandler<AudioEventDetection>? AudioEventDetected; // Event for specific audio events
        
        // Audio processing parameters
        private const int SAMPLE_RATE = 44100;
        private const int CHANNELS = 2; // Stereo
        private const int WINDOW_SIZE_MS = 50; // 50ms windows
        private int samplesPerWindow;
        
        // Buffers for audio data
        private float[] leftBuffer;
        private float[] rightBuffer;
        private int bufferIndex;

        public AudioAnalyzer()
        {
            samplesPerWindow = (SAMPLE_RATE * WINDOW_SIZE_MS) / 1000;
            leftBuffer = new float[samplesPerWindow];
            rightBuffer = new float[samplesPerWindow];
            bufferIndex = 0;
            
            // Initialize FFT window (Hanning window)
            for (int i = 0; i < FFT_SIZE; i++)
            {
                fftWindow[i] = 0.5f * (1 - (float)Math.Cos(2.0 * Math.PI * i / (FFT_SIZE - 1)));
            }
            
            InitializeEventProfiles();
        }

        public void Start(string? deviceId = null)
        {
            if (isRunning)
                return;

            try
            {
                // Get the audio device to use
                GetAudioDevice(deviceId);
                
                // Initialize WASAPI loopback capture with the selected device
                if (selectedDevice != null)
                {
                    capture = new WasapiLoopbackCapture(selectedDevice);
                }
                else
                {
                    capture = new WasapiLoopbackCapture(); // Use default device
                }
                
                // Subscribe to data available event
                capture.DataAvailable += OnDataAvailable;
                capture.RecordingStopped += OnRecordingStopped;
                
                // Start recording
                capture.StartRecording();
                isRunning = true;
                
                // Notify about the device being used
                string deviceName = selectedDevice?.FriendlyName ?? "Default Device";
                DeviceChanged?.Invoke(this, $"Using device: {deviceName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start audio capture: {ex.Message}", ex);
            }
        }

        private void GetAudioDevice(string? deviceId = null)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            
            // Use the provided device ID or fall back to the property
            string? targetDeviceId = deviceId ?? SelectedDeviceId;
            
            if (!string.IsNullOrEmpty(targetDeviceId))
            {
                try
                {
                    // Try to get the specific device by ID
                    selectedDevice = deviceEnumerator.GetDevice(targetDeviceId);
                    return;
                }
                catch
                {
                    // If the specific device is not available, fall back to default
                }
            }
            
            // Use default device
            try
            {
                selectedDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
            catch
            {
                selectedDevice = null; // Will use NAudio's default behavior
            }
        }

        public static List<AudioDevice> GetAvailableDevices()
        {
            var devices = new List<AudioDevice>();
            var deviceEnumerator = new MMDeviceEnumerator();
            
            try
            {
                // Add default device option
                devices.Add(new AudioDevice 
                { 
                    Id = "", 
                    Name = "Default Device", 
                    IsDefault = true 
                });
                
                // Get all render devices (playback devices)
                var deviceCollection = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                foreach (var device in deviceCollection)
                {
                    devices.Add(new AudioDevice
                    {
                        Id = device.ID,
                        Name = device.FriendlyName,
                        IsDefault = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enumerating devices: {ex.Message}");
            }
            
            return devices;
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            
            try
            {
                capture?.StopRecording();
            }
            catch (Exception)
            {
                // Ignore exceptions during stop
            }
        }

        public void StartMonitoring(string? deviceId = null)
        {
            Start(deviceId);
        }

        public void StopMonitoring()
        {
            Stop();
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!isRunning || e.BytesRecorded == 0)
                return;

            try
            {
                // The audio format depends on the capture device, but typically it's 32-bit float
                // Check if we have the expected format
                if (capture?.WaveFormat == null)
                    return;

                int bytesPerSample = capture.WaveFormat.BitsPerSample / 8;
                int channels = capture.WaveFormat.Channels;
                
                // Process audio based on format
                if (bytesPerSample == 4) // 32-bit float
                {
                    ProcessFloatAudio(e.Buffer, e.BytesRecorded, channels);
                }
                else if (bytesPerSample == 2) // 16-bit PCM
                {
                    ProcessInt16Audio(e.Buffer, e.BytesRecorded, channels);
                }
            }
            catch (Exception)
            {
                // Continue processing even if there are occasional errors
            }
        }

        private void ProcessFloatAudio(byte[] buffer, int bytesRecorded, int channels)
        {
            int totalSamples = bytesRecorded / 4; // Total samples (all channels)
            int framesCount = totalSamples / channels; // Number of frames (one frame = all channels)
            
            for (int frame = 0; frame < framesCount && bufferIndex < samplesPerWindow; frame++)
            {
                // Calculate byte offset for this frame
                int frameOffset = frame * channels * 4; // 4 bytes per float
                
                // Ensure we don't exceed buffer bounds
                if (frameOffset + (channels * 4) > bytesRecorded) break;
                
                // Left channel (always present at offset 0 of each frame)
                float leftSample = BitConverter.ToSingle(buffer, frameOffset);
                
                // Right channel handling
                float rightSample;
                if (channels >= 2)
                {
                    // Stereo - get right channel at offset 4 bytes from left
                    rightSample = BitConverter.ToSingle(buffer, frameOffset + 4);
                }
                else
                {
                    // Mono - duplicate left to right
                    rightSample = leftSample;
                }
                
                // Add to buffers with absolute values
                leftBuffer[bufferIndex] = Math.Abs(leftSample);
                rightBuffer[bufferIndex] = Math.Abs(rightSample);
                bufferIndex++;
                
                // Process window when buffer is full
                if (bufferIndex >= samplesPerWindow)
                {
                    ProcessAudioWindow();
                    bufferIndex = 0;
                }
            }
        }

        private void ProcessInt16Audio(byte[] buffer, int bytesRecorded, int channels)
        {
            int totalSamples = bytesRecorded / 2; // Total samples (all channels)
            int framesCount = totalSamples / channels; // Number of frames (one frame = all channels)
            
            for (int frame = 0; frame < framesCount && bufferIndex < samplesPerWindow; frame++)
            {
                // Calculate byte offset for this frame
                int frameOffset = frame * channels * 2; // 2 bytes per int16
                
                // Ensure we don't exceed buffer bounds
                if (frameOffset + (channels * 2) > bytesRecorded) break;
                
                // Left channel (always present at offset 0 of each frame)
                float leftSample = BitConverter.ToInt16(buffer, frameOffset) / 32768.0f;
                
                // Right channel handling
                float rightSample;
                if (channels >= 2)
                {
                    // Stereo - get right channel at offset 2 bytes from left
                    rightSample = BitConverter.ToInt16(buffer, frameOffset + 2) / 32768.0f;
                }
                else
                {
                    // Mono - duplicate left to right
                    rightSample = leftSample;
                }
                
                // Add to buffers with absolute values
                leftBuffer[bufferIndex] = Math.Abs(leftSample);
                rightBuffer[bufferIndex] = Math.Abs(rightSample);
                bufferIndex++;
                
                // Process window when buffer is full
                if (bufferIndex >= samplesPerWindow)
                {
                    ProcessAudioWindow();
                    bufferIndex = 0;
                }
            }
        }

        private void ProcessAudioWindow()
        {
            try
            {
                // Calculate RMS for left and right channels using enhanced method
                float leftRMS = CalculateEnhancedRMS(leftBuffer);
                float rightRMS = CalculateEnhancedRMS(rightBuffer);
                
                // Update noise floor for adaptive threshold
                UpdateNoiseFloor(leftRMS, rightRMS);
                
                // Get effective threshold (adaptive or fixed)
                float effectiveThreshold = GetEffectiveThreshold();
                
                // Check which channels exceed the threshold
                bool leftExceedsThreshold = leftRMS > effectiveThreshold;
                bool rightExceedsThreshold = rightRMS > effectiveThreshold;
                
                // Add some debugging info
                System.Diagnostics.Debug.WriteLine($"Left RMS: {leftRMS:F4}, Right RMS: {rightRMS:F4}");
                System.Diagnostics.Debug.WriteLine($"Threshold: {VolumeThreshold:F4}, Effective: {effectiveThreshold:F4}, Mode: {TriggerMode}");
                System.Diagnostics.Debug.WriteLine($"Sensitivity: {SensitivityMode}, Gain: {AudioGain:F2}, Noise Floor: {currentNoiseFloor:F4}");
                System.Diagnostics.Debug.WriteLine($"Left Enabled: {LeftChannelEnabled}, Right Enabled: {RightChannelEnabled}");
                System.Diagnostics.Debug.WriteLine($"Left Exceeds: {leftExceedsThreshold}, Right Exceeds: {rightExceedsThreshold}");
                
                // Perform frequency analysis if enabled
                if (EnableFrequencyAnalysis)
                {
                    PerformFFT(leftBuffer, true);   // Left channel
                    PerformFFT(rightBuffer, false); // Right channel
                }
                
                // Apply trigger mode logic
                switch (TriggerMode)
                {
                    case ChannelTriggerMode.Independent:
                        // Both channels can trigger independently
                        if (leftExceedsThreshold && LeftChannelEnabled)
                        {
                            LeftChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering LEFT (independent)");
                        }
                        if (rightExceedsThreshold && RightChannelEnabled)
                        {
                            RightChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering RIGHT (independent)");
                        }
                        break;
                        
                    case ChannelTriggerMode.Exclusive:
                        // Only trigger the louder channel, or single channel if only one is active
                        if (leftExceedsThreshold && rightExceedsThreshold && LeftChannelEnabled && RightChannelEnabled)
                        {
                            // Both channels are active, trigger only the louder one
                            if (leftRMS >= rightRMS)
                            {
                                LeftChannelActive?.Invoke(this, EventArgs.Empty);
                                System.Diagnostics.Debug.WriteLine("Triggering LEFT (exclusive - louder)");
                            }
                            else
                            {
                                RightChannelActive?.Invoke(this, EventArgs.Empty);
                                System.Diagnostics.Debug.WriteLine("Triggering RIGHT (exclusive - louder)");
                            }
                        }
                        else if (leftExceedsThreshold && LeftChannelEnabled)
                        {
                            // Only left channel is active
                            LeftChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering LEFT (exclusive - only)");
                        }
                        else if (rightExceedsThreshold && RightChannelEnabled)
                        {
                            // Only right channel is active
                            RightChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering RIGHT (exclusive - only)");
                        }
                        break;
                        
                    case ChannelTriggerMode.Threshold:
                        // Only trigger channels that are significantly louder than the other
                        if (leftExceedsThreshold && LeftChannelEnabled && rightExceedsThreshold && RightChannelEnabled)
                        {
                            // Both exceed threshold, check if one is significantly louder
                            float difference = Math.Abs(leftRMS - rightRMS);
                            if (difference >= ChannelSeparationThreshold)
                            {
                                if (leftRMS > rightRMS)
                                {
                                    LeftChannelActive?.Invoke(this, EventArgs.Empty);
                                    System.Diagnostics.Debug.WriteLine("Triggering LEFT (threshold - significantly louder)");
                                }
                                else
                                {
                                    RightChannelActive?.Invoke(this, EventArgs.Empty);
                                    System.Diagnostics.Debug.WriteLine("Triggering RIGHT (threshold - significantly louder)");
                                }
                            }
                            // If difference is too small, don't trigger either (avoid dual activation)
                        }
                        else if (leftExceedsThreshold && LeftChannelEnabled)
                        {
                            // Only left channel exceeds threshold
                            LeftChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering LEFT (threshold - only)");
                        }
                        else if (rightExceedsThreshold && RightChannelEnabled)
                        {
                            // Only right channel exceeds threshold
                            RightChannelActive?.Invoke(this, EventArgs.Empty);
                            System.Diagnostics.Debug.WriteLine("Triggering RIGHT (threshold - only)");
                        }
                        break;
                }
            }
            catch (Exception)
            {
                // Continue processing even if there are occasional errors
            }
        }

        private float CalculateRMS(float[] samples)
        {
            if (samples.Length == 0)
                return 0;

            double sum = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            
            float rms = (float)Math.Sqrt(sum / samples.Length);
            
            // Apply audio gain amplification
            rms *= AudioGain;
            
            return rms;
        }
        
        private float CalculateEnhancedRMS(float[] samples)
        {
            if (samples.Length == 0)
                return 0;

            float rms = CalculateRMS(samples);
            
            // Apply sensitivity mode enhancements
            switch (SensitivityMode)
            {
                case 1: // High sensitivity
                    rms = ApplyHighSensitivity(rms, samples);
                    break;
                case 2: // Ultra-high sensitivity  
                    rms = ApplyUltraHighSensitivity(rms, samples);
                    break;
            }
            
            return rms;
        }
        
        private float ApplyHighSensitivity(float rms, float[] samples)
        {
            // Use peak detection for high sensitivity
            float peak = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] > peak)
                    peak = samples[i];
            }
            
            // Combine RMS and peak for better sensitivity
            float enhanced = (rms * 0.7f) + (peak * 0.3f);
            enhanced *= 2.0f; // Boost sensitivity
            
            return enhanced;
        }
        
        private float ApplyUltraHighSensitivity(float rms, float[] samples)
        {
            // Calculate multiple metrics
            float peak = samples.Max();
            float average = samples.Average();
            
            // Calculate dynamic range
            float dynamicRange = peak - currentNoiseFloor;
            
            // Enhanced calculation using multiple factors
            float enhanced = (rms * 0.5f) + (peak * 0.3f) + (dynamicRange * 0.2f);
            enhanced *= 3.0f; // Strong boost for ultra sensitivity
            
            // Apply logarithmic scaling for very quiet sounds
            if (enhanced < 0.01f && enhanced > 0)
            {
                enhanced = (float)(Math.Log10(enhanced * 100 + 1) * 0.1f);
            }
            
            return enhanced;
        }
        
        private void UpdateNoiseFloor(float leftRMS, float rightRMS)
        {
            if (!UseAdaptiveThreshold)
                return;
                
            // Use the minimum of both channels as potential noise floor
            float currentNoise = Math.Min(leftRMS, rightRMS);
            
            // Add to noise history
            noiseHistory[noiseHistoryIndex] = currentNoise;
            noiseHistoryIndex = (noiseHistoryIndex + 1) % noiseHistory.Length;
            
            // Calculate new noise floor as average of lowest 20% of samples
            var sortedNoise = noiseHistory.OrderBy(x => x).Take(20).ToArray();
            if (sortedNoise.Length > 0)
            {
                currentNoiseFloor = sortedNoise.Average();
                // Ensure minimum noise floor
                if (currentNoiseFloor < NoiseFloor)
                    currentNoiseFloor = NoiseFloor;
            }
        }
        
        private float GetEffectiveThreshold()
        {
            if (UseAdaptiveThreshold)
            {
                // Adaptive threshold is noise floor + margin
                return currentNoiseFloor + (VolumeThreshold * 0.5f);
            }
            else
            {
                return VolumeThreshold;
            }
        }

        private void InitializeEventProfiles()
        {
            eventProfiles.Clear();
            
            // Footsteps - Low to mid frequency, brief duration
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.Footsteps,
                Name = "Footsteps",
                MinFrequency = 20f,
                MaxFrequency = 2000f,
                PeakFrequency = 200f,
                MinIntensity = 0.05f,
                Duration = 200f, // 200ms
                RequiresTransient = true,
                SpectralCentroid = 800f
            });
            
            // Gunshots - Sharp transient with mid-high frequency
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.Gunshot,
                Name = "Gunshot",
                MinFrequency = 500f,
                MaxFrequency = 8000f,
                PeakFrequency = 2000f,
                MinIntensity = 0.3f,
                Duration = 50f, // Very brief
                RequiresTransient = true,
                SpectralCentroid = 3000f
            });
            
            // Explosions - Wide frequency range, longer duration
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.Explosion,
                Name = "Explosion",
                MinFrequency = 20f,
                MaxFrequency = 10000f,
                PeakFrequency = 150f,
                MinIntensity = 0.4f,
                Duration = 500f,
                RequiresTransient = true,
                SpectralCentroid = 1000f
            });
            
            // Voice/Shouts - Human vocal range
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.VoiceShout,
                Name = "Voice/Shout",
                MinFrequency = 85f,
                MaxFrequency = 4000f,
                PeakFrequency = 1000f,
                MinIntensity = 0.1f,
                Duration = 800f,
                RequiresTransient = false,
                SpectralCentroid = 1500f
            });
            
            // Metallic sounds - High frequency content
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.Metallic,
                Name = "Metallic",
                MinFrequency = 2000f,
                MaxFrequency = 15000f,
                PeakFrequency = 6000f,
                MinIntensity = 0.1f,
                Duration = 300f,
                RequiresTransient = true,
                SpectralCentroid = 8000f
            });
            
            // Glass breaking - Very high frequencies
            eventProfiles.Add(new AudioEventProfile
            {
                EventType = AudioEventType.Glass,
                Name = "Glass Breaking",
                MinFrequency = 3000f,
                MaxFrequency = 20000f,
                PeakFrequency = 8000f,
                MinIntensity = 0.15f,
                Duration = 400f,
                RequiresTransient = true,
                SpectralCentroid = 10000f
            });
        }

        private void PerformFFT(float[] audioSamples, bool isLeftChannel)
        {
            if (!EnableFrequencyAnalysis) return;
            
            // Add samples to FFT buffer
            for (int i = 0; i < audioSamples.Length && fftBufferIndex < FFT_SIZE; i++)
            {
                fftBuffer[fftBufferIndex] = audioSamples[i] * fftWindow[fftBufferIndex];
                fftBufferIndex++;
                
                if (fftBufferIndex >= FFT_SIZE)
                {
                    // Perform FFT analysis
                    CalculateFrequencySpectrum();
                    
                    if (EnableEventDetection)
                    {
                        DetectAudioEvents(isLeftChannel);
                    }
                    
                    // Reset buffer for next analysis
                    fftBufferIndex = 0;
                }
            }
        }
        
        private void CalculateFrequencySpectrum()
        {
            // Simple magnitude spectrum calculation
            // For production, you'd use a proper FFT library like FFTW or similar
            
            // Clear frequency bins
            Array.Clear(frequencyBins, 0, frequencyBins.Length);
            
            // Calculate frequency magnitudes (simplified)
            for (int i = 0; i < FFT_SIZE / 2; i++)
            {
                float real = 0f;
                float imag = 0f;
                
                // Simple DFT calculation for demonstration
                for (int n = 0; n < FFT_SIZE; n++)
                {
                    float angle = -2.0f * (float)Math.PI * i * n / FFT_SIZE;
                    real += fftBuffer[n] * (float)Math.Cos(angle);
                    imag += fftBuffer[n] * (float)Math.Sin(angle);
                }
                
                // Magnitude
                frequencyBins[i] = (float)Math.Sqrt(real * real + imag * imag) / FFT_SIZE;
            }
        }
        
        private void DetectAudioEvents(bool isLeftChannel)
        {
            float sampleRate = SAMPLE_RATE;
            float binWidth = sampleRate / FFT_SIZE;
            
            foreach (var profile in eventProfiles)
            {
                float confidence = AnalyzeEventProfile(profile, binWidth);
                
                if (confidence >= EventDetectionSensitivity)
                {
                    var detection = new AudioEventDetection
                    {
                        EventType = profile.EventType,
                        Confidence = confidence,
                        Frequency = profile.PeakFrequency,
                        Intensity = GetFrequencyMagnitude(profile.PeakFrequency, binWidth),
                        IsLeftChannel = isLeftChannel,
                        Timestamp = DateTime.Now
                    };
                    
                    AudioEventDetected?.Invoke(this, detection);
                    
                    System.Diagnostics.Debug.WriteLine($"Audio Event Detected: {profile.Name}, Confidence: {confidence:F2}, Channel: {(isLeftChannel ? "Left" : "Right")}");
                }
            }
        }
        
        private float AnalyzeEventProfile(AudioEventProfile profile, float binWidth)
        {
            float confidence = 0f;
            float totalEnergy = 0f;
            float profileEnergy = 0f;
            
            // Calculate energy in the profile's frequency range
            int minBin = (int)(profile.MinFrequency / binWidth);
            int maxBin = (int)(profile.MaxFrequency / binWidth);
            int peakBin = (int)(profile.PeakFrequency / binWidth);
            
            minBin = Math.Max(0, Math.Min(minBin, frequencyBins.Length - 1));
            maxBin = Math.Max(0, Math.Min(maxBin, frequencyBins.Length - 1));
            peakBin = Math.Max(0, Math.Min(peakBin, frequencyBins.Length - 1));
            
            // Calculate energy in profile range vs total energy
            for (int i = 0; i < frequencyBins.Length; i++)
            {
                totalEnergy += frequencyBins[i];
                if (i >= minBin && i <= maxBin)
                {
                    profileEnergy += frequencyBins[i];
                }
            }
            
            if (totalEnergy > 0)
            {
                float energyRatio = profileEnergy / totalEnergy;
                
                // Check if peak frequency has significant energy
                float peakEnergy = frequencyBins[peakBin];
                
                // Check if minimum intensity is met
                if (peakEnergy >= profile.MinIntensity)
                {
                    // Calculate confidence based on multiple factors
                    confidence = energyRatio * 0.6f + (peakEnergy / totalEnergy) * 0.4f;
                    
                    // Bonus for transient events (high peak-to-average ratio)
                    if (profile.RequiresTransient)
                    {
                        float averageEnergy = profileEnergy / (maxBin - minBin + 1);
                        if (peakEnergy > averageEnergy * 2) // Peak is at least 2x average
                        {
                            confidence *= 1.2f; // 20% bonus
                        }
                    }
                }
            }
            
            return Math.Min(confidence, 1.0f);
        }
        
        private float GetFrequencyMagnitude(float frequency, float binWidth)
        {
            int bin = (int)(frequency / binWidth);
            bin = Math.Max(0, Math.Min(bin, frequencyBins.Length - 1));
            return frequencyBins[bin];
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                // Log or handle the exception as needed
                System.Diagnostics.Debug.WriteLine($"Recording stopped with exception: {e.Exception.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            
            if (capture != null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
                capture = null;
            }
            
            selectedDevice?.Dispose();
            selectedDevice = null;
        }
    }

    public class AudioDevice
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsDefault { get; set; }
    }
}
