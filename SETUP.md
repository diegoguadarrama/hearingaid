# HearingAI Setup and Usage Guide

## Prerequisites

Before running HearingAI, you need to install the .NET 8.0 SDK or Runtime:

1. **Download .NET 8.0**: Visit https://dotnet.microsoft.com/download/dotnet/8.0
2. **Choose the appropriate installer**:
   - For development: Download the .NET 8.0 SDK
   - For running only: Download the .NET 8.0 Runtime (Desktop Apps)
3. **Install**: Run the installer and follow the instructions
4. **Verify installation**: Open Command Prompt and type `dotnet --version`

## Building the Application

### Option 1: Using the Build Script (Recommended)
1. Double-click `build.bat` in the project folder
2. The script will check for .NET SDK and build the project automatically

### Option 2: Using Command Line
1. Open Command Prompt in the project folder
2. Run: `dotnet restore` (to download dependencies)
3. Run: `dotnet build --configuration Release` (to build the project)

### Option 3: Using Visual Studio
1. Open `HearingAI.csproj` in Visual Studio 2022
2. Build > Build Solution (or press Ctrl+Shift+B)

## Running the Application

### Option 1: Using the Run Script
- Double-click `run.bat` in the project folder

### Option 2: Using Command Line
- Run: `dotnet run --configuration Release`

### Option 3: Running the Executable
- Navigate to `bin\Release\net8.0-windows\`
- Double-click `HearingAI.exe`

## Using the Application

1. **Configure Settings**:
   - Volume Threshold: Adjust how sensitive the app is to audio (1-100%)
   - Flash Duration: How long the visual alerts last (50-2000ms)
   - Colors: Choose different colors for left and right channel alerts

2. **Start Monitoring**:
   - Click "Start Monitoring" to begin audio capture
   - The app will show visual alerts on screen edges when audio is detected

3. **System Tray**:
   - Check "Minimize to system tray" to run in background
   - Right-click the tray icon to show/hide or exit

## Troubleshooting

### .NET SDK Not Found
- Install .NET 8.0 SDK from Microsoft's website
- Restart Command Prompt after installation

### Audio Not Working
- Ensure audio is playing on your computer
- Try adjusting the volume threshold (lower values = more sensitive)
- Check Windows audio settings

### Visual Alerts Not Showing
- Ensure the overlay window is not hidden behind other windows
- Try different flash colors if they're hard to see
- Check flash duration setting

### Build Errors
- Ensure you have .NET 8.0 SDK installed (not just runtime)
- Make sure all project files are present
- Try deleting `bin` and `obj` folders and rebuilding

## Technical Notes

- The app uses WASAPI loopback capture to monitor system audio
- Visual alerts are displayed using a transparent overlay window
- Settings are automatically saved to your user profile
- The app requires Windows 10 or later
