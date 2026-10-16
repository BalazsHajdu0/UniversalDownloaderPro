@echo off
echo ========================================
echo  Universal Downloader Pro - Build Script
echo ========================================
echo.

:: Find MSBuild
set MSBUILD=
for %%i in (
    "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2026\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2026\Professional\MSBuild\Current\Bin\MSBuild.exe"
    "%ProgramFiles%\Microsoft Visual Studio\2026\Community\MSBuild\Current\Bin\MSBuild.exe"
) do (
    if exist %%i set MSBUILD=%%i
)

if "%MSBUILD%"=="" (
    echo ERROR: MSBuild not found!
    echo Please install Visual Studio 2022 or 2026 with .NET Desktop Development workload.
    pause
    exit /b 1
)

echo Found MSBuild: %MSBUILD%
echo.

:: Build Release
echo Building Release...
%MSBUILD% UniversalDownloaderWPF.sln /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo ========================================
echo  BUILD SUCCESSFUL!
echo ========================================
echo.
echo Output: UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe
echo.

:: Check for tools
set TOOLS_DIR=UniversalDownloaderWPF\bin\Release\Tools
if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"

echo Checking tools...
if not exist "%TOOLS_DIR%\yt-dlp.exe" (
    echo [!] yt-dlp.exe missing - downloading...
    powershell -Command "Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile '%TOOLS_DIR%\yt-dlp.exe'"
    if exist "%TOOLS_DIR%\yt-dlp.exe" (
        echo [OK] yt-dlp.exe downloaded
    ) else (
        echo [!] Failed to download yt-dlp.exe
    )
) else (
    echo [OK] yt-dlp.exe found
)

if not exist "%TOOLS_DIR%\ffmpeg.exe" (
    echo [!] ffmpeg.exe missing
    echo     Please download from: https://www.gyan.dev/ffmpeg/builds/
    echo     Extract ffmpeg.exe to: %TOOLS_DIR%\
) else (
    echo [OK] ffmpeg.exe found
)

echo.
echo ========================================
echo  Next steps:
echo ========================================
echo 1. Copy ffmpeg.exe to Tools folder (if missing)
echo 2. Run the app: UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe
echo 3. Or build installer: Run Installer\setup.iss with Inno Setup
echo.
pause
