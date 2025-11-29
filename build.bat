@echo off
echo Building HearingAI Application...
echo.

REM Set the full path to dotnet
set DOTNET_PATH="C:\Program Files\dotnet\dotnet.exe"

REM Check if .NET SDK is installed
%DOTNET_PATH% --version >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Restoring NuGet packages...
%DOTNET_PATH% restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo Building project...
%DOTNET_PATH% build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo You can run the application with: run.bat
echo Or find the executable in: bin\Release\net8.0-windows\
pause
