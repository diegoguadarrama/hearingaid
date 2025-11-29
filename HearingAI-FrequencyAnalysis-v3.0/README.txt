HearingAI - Audio Visual Alert System with Frequency Analysis
=============================================================

This is a standalone executable that does not require .NET Runtime to be installed.

SYSTEM REQUIREMENTS:
- Windows 10 or later (64-bit)
- Audio playback device (speakers/headphones)

INSTALLATION:
1. Simply run HearingAI.exe - no installation required
2. The application may ask for microphone/audio permissions - this is normal
3. Settings are automatically saved in your user profile

FIRST TIME SETUP:
1. Run HearingAI.exe
2. Adjust the Volume Threshold (start with 5-10%)
3. Choose your preferred flash colors for left/right channels
4. For gaming: Try "High" sensitivity mode with 2-3x Audio Gain
5. Click "Start Monitoring"

ENHANCED DETECTION FEATURES:
- Audio Gain: Amplifies quiet sounds (useful for games)
- Sensitivity Modes: Normal/High/Ultra-High for different use cases
- Adaptive Threshold: Auto-adjusts based on background noise
- Channel Trigger Modes: Independent/Exclusive/Threshold

NEW! FREQUENCY ANALYSIS & EVENT DETECTION:
- Enable Frequency Analysis: Analyzes audio frequencies in real-time
- Audio Event Detection: Automatically detects specific sounds:
  * Footsteps (20-2000 Hz, transient)
  * Gunshots (500-8000 Hz, sharp transient)
  * Explosions (20-10000 Hz, wide spectrum)
  * Voice/Shouts (85-4000 Hz, human vocal range)
  * Metallic sounds (2000-15000 Hz, high frequency)
  * Glass breaking (3000-20000 Hz, very high frequency)
- Event Detection Sensitivity: Adjustable from 10% to 100%
- Real-time Event Log: Shows detected events with timestamps

GAMING OPTIMIZATION:
For detecting footsteps, gunshots, and other game audio:
1. Enable "Frequency Analysis" 
2. Enable "Audio Event Detection"
3. Set Event Sensitivity to 60-80%
4. Use High sensitivity mode with 2-3x Audio Gain
5. Enable Adaptive Threshold for background noise filtering

TROUBLESHOOTING:
- If no flashing occurs, try increasing Audio Gain or using High sensitivity
- For games, enable "Use Adaptive Threshold" to ignore background music
- Check that the correct audio device is selected in settings
- Enable Frequency Analysis for specific sound detection (footsteps, gunshots)
- Lower Event Detection Sensitivity if getting too many false positives

TECHNICAL DETAILS:
- Uses FFT (Fast Fourier Transform) for frequency analysis
- 1024-point FFT with Hanning window
- Real-time spectral analysis at 44.1 kHz sample rate
- Pattern matching for specific audio signatures
- Directional audio support (left/right channel detection)

VERSION: Enhanced Detection with Frequency Analysis v3.0
BUILD DATE: September 8, 2025
