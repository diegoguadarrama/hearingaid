# HearingAI - Audio Visual Alert System

A Windows desktop application that provides visual alerts for people who are hard of hearing by monitoring system audio and displaying colored flashes on screen edges when left or right audio channels are active.

## Features

- **Real-time Audio Monitoring**: Captures all system audio using NAudio's WASAPI loopback
- **Channel-specific Alerts**: Separate visual alerts for left and right audio channels
- **Customizable Settings**: 
  - Adjustable volume threshold
  - Configurable flash duration (50-2000ms)
  - Custom colors for left and right channel alerts
- **Transparent Overlay**: Borderless, always-on-top window with click-through capability
- **System Tray Support**: Minimize to system tray for background operation
- **User-friendly Interface**: Simple settings form for easy configuration

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Audio output device

## Installation

1. Download the latest release from the releases page
2. Extract the files to a directory of your choice
3. Run `HearingAI.exe`

## Building from Source

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK

### Steps
1. Clone this repository
2. Open `HearingAI.csproj` in Visual Studio
3. Restore NuGet packages (NAudio will be automatically downloaded)
4. Build and run the project

## Usage

1. **Start the Application**: Launch `HearingAI.exe`
2. **Configure Settings**:
   - Adjust the volume threshold slider (1-100%)
   - Set flash duration in milliseconds
   - Choose colors for left and right channel alerts
3. **Start Monitoring**: Click "Start Monitoring" to begin audio analysis
4. **Visual Alerts**: Red rectangles will flash on the left/right edges of your screen when audio activity is detected in the corresponding channels
5. **Minimize to Tray**: Check the minimize to tray option to run in the background

## How It Works

The application uses NAudio's WasapiLoopbackCapture to monitor all system audio output. It processes audio in 50ms windows, calculating RMS (Root Mean Square) values for both left and right channels. When a channel's volume exceeds the configured threshold, a colored rectangle flashes on the corresponding edge of the screen.

The overlay window is completely transparent except for the flash alerts and allows mouse clicks to pass through, ensuring it doesn't interfere with normal computer usage.

## Technical Details

- **Audio Processing**: 44.1kHz stereo audio processing with configurable thresholds
- **UI Framework**: Windows Forms with custom transparent overlay
- **Performance**: Optimized for low CPU usage and minimal memory footprint
- **Thread Safety**: Proper synchronization between audio processing and UI threads

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [NAudio](https://github.com/naudio/NAudio) - Audio capture and processing library
- Built for the hearing-impaired community to improve accessibility
