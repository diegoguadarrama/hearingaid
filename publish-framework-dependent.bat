@echo off
echo Publishing HearingAI as framework-dependent deployment...
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

echo Creating framework-dependent deployment (requires .NET runtime)...
%DOTNET_PATH% publish -c Release -r win-x64 --self-contained false --output "dist\framework-dependent"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)

echo.
echo Framework-dependent deployment created successfully!
echo Location: dist\framework-dependent\
echo File size: Small (~5MB) but requires .NET runtime on target machine
echo.
pause
