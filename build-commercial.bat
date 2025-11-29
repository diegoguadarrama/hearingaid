@echo off
echo HearingAI Complete Build and Package Script
echo ==========================================
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

echo Step 1: Building project...
%DOTNET_PATH% build --configuration Release --verbosity minimal
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed - check error messages above
    pause
    exit /b 1
)
echo Build successful!

echo Step 2: Creating standalone deployment...
%DOTNET_PATH% publish -c Release -r win-x64 --self-contained true --output "dist\standalone" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Standalone publish failed
    pause
    exit /b 1
)

echo Step 3: Creating framework-dependent deployment...
%DOTNET_PATH% publish -c Release -r win-x64 --self-contained false --output "dist\framework-dependent"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Framework-dependent publish failed
    pause
    exit /b 1
)

echo Step 4: Creating distribution packages...
if not exist "dist\packages" mkdir "dist\packages"

REM Create ZIP for standalone version
if exist "%ProgramFiles%\7-Zip\7z.exe" (
    "%ProgramFiles%\7-Zip\7z.exe" a "dist\packages\HearingAI-v1.0.0-Standalone.zip" "dist\standalone\*" "README.md" "LICENSE*"
    echo Created standalone ZIP package
) else (
    echo 7-Zip not found - manual ZIP creation needed for dist\standalone\
)

REM Create ZIP for framework-dependent version
if exist "%ProgramFiles%\7-Zip\7z.exe" (
    "%ProgramFiles%\7-Zip\7z.exe" a "dist\packages\HearingAI-v1.0.0-Framework-Dependent.zip" "dist\framework-dependent\*" "README.md" "LICENSE*"
    echo Created framework-dependent ZIP package
)

echo.
echo ==========================================
echo Build Complete!
echo ==========================================
echo.
echo Deployments created:
echo 1. Standalone (no .NET required): dist\standalone\
echo    - File: HearingAI.exe (~100MB)
echo    - Users can run without installing .NET
echo.
echo 2. Framework-dependent (.NET required): dist\framework-dependent\
echo    - File: HearingAI.exe (~5MB)  
echo    - Users need .NET 8.0 Runtime installed
echo.
echo 3. Distribution packages: dist\packages\
echo    - HearingAI-v1.0.0-Standalone.zip
echo    - HearingAI-v1.0.0-Framework-Dependent.zip
echo.
echo Next steps for commercialization:
echo 1. Test both deployments on clean machines
echo 2. Get code signing certificate ($200-400/year)
echo 3. Create professional installer with NSIS (installer.nsi provided)
echo 4. Set up payment processing and website
echo 5. Consider Microsoft Store submission
echo.
echo See COMMERCIAL_GUIDE.md for detailed commercialization strategy
echo.
pause
