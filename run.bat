@echo off
echo Running HearingAI Application...
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

echo Starting application...
%DOTNET_PATH% run --configuration Release
