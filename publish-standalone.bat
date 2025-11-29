@echo off
echo Publishing HearingAI as standalone executable...
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

echo Creating self-contained deployment (includes .NET runtime)...
%DOTNET_PATH% publish -c Release -r win-x64 --self-contained true --output "dist\standalone" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)

echo.
echo Standalone deployment created successfully!
echo Location: dist\standalone\
echo File size: Large (~100MB) but includes everything needed
echo Users don't need .NET installed
echo.
pause
