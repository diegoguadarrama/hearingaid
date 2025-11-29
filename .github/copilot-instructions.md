<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# HearingAI Project Instructions

This is a Windows desktop application built with C# .NET and WinForms that provides visual alerts for people who are hard of hearing.

## Project Structure
- **Program.cs**: Application entry point
- **SettingsForm.cs**: Main UI form for configuration and control
- **OverlayWindow.cs**: Transparent overlay window that shows visual alerts
- **AudioAnalyzer.cs**: Audio capture and analysis using NAudio

## Key Technologies
- **.NET 8.0** with Windows Forms
- **NAudio** for audio capture using WASAPI loopback
- **WinAPI** for creating click-through overlay windows

## Development Guidelines
1. Use async/await patterns for UI responsiveness
2. Proper disposal of audio resources and timers
3. Thread-safe UI updates using Control.Invoke
4. Error handling for audio device access
5. Memory-efficient audio processing with fixed-size buffers

## Audio Processing
- Captures system audio using WasapiLoopbackCapture
- Processes stereo audio in 50ms windows
- Calculates RMS values for left/right channels
- Triggers visual alerts when volume exceeds threshold

## UI Features
- Settings form with volume threshold, flash duration, and color configuration
- System tray support for background operation
- Real-time visual feedback overlay
- Click-through transparent overlay windows
