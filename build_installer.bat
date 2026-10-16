@echo off
echo ========================================
echo  Universal Downloader Pro - Installer Build
echo ========================================
echo.

:: Check if Inno Setup is installed
set ISCC=
for %%i in (
    "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    "%ProgramFiles%\Inno Setup 6\ISCC.exe"
) do (
    if exist %%i set ISCC=%%i
)

if "%ISCC%"=="" (
    echo ERROR: Inno Setup not found!
    echo Please download and install from: https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

echo Found Inno Setup: %ISCC%
echo.

:: Check if app is built
if not exist "UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe" (
    echo ERROR: Application not built!
    echo Please run build.bat first.
    pause
    exit /b 1
)

:: Create Tools folder in Installer directory
if not exist "Installer\Tools" mkdir "Installer\Tools"

:: Copy tools from Release to Installer
echo Copying tools...
if exist "UniversalDownloaderWPF\bin\Release\Tools\yt-dlp.exe" (
    copy /Y "UniversalDownloaderWPF\bin\Release\Tools\yt-dlp.exe" "Installer\Tools\" >nul
    echo [OK] yt-dlp.exe copied
) else (
    echo [!] yt-dlp.exe not found - downloading...
    powershell -Command "Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -OutFile 'Installer\Tools\yt-dlp.exe'"
)

if exist "UniversalDownloaderWPF\bin\Release\Tools\ffmpeg.exe" (
    copy /Y "UniversalDownloaderWPF\bin\Release\Tools\ffmpeg.exe" "Installer\Tools\" >nul
    echo [OK] ffmpeg.exe copied
) else (
    echo [!] WARNING: ffmpeg.exe not found!
    echo     The installer will be created without ffmpeg.
    echo     Users will need to download it manually.
    echo.
)

:: Check if tools exist for installer
if not exist "Installer\Tools\yt-dlp.exe" (
    echo ERROR: yt-dlp.exe is required for the installer!
    pause
    exit /b 1
)

:: Build installer
echo.
echo Building installer...
"%ISCC%" "Installer\setup.iss"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo INSTALLER BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo ========================================
echo  INSTALLER BUILD SUCCESSFUL!
echo ========================================
echo.
echo Output: Installer\Output\UniversalDownloaderSetup.exe
echo.
echo This installer includes:
echo  - Universal Downloader Pro application
echo  - yt-dlp.exe
if exist "Installer\Tools\ffmpeg.exe" (
    echo  - ffmpeg.exe
) else (
    echo  - [!] ffmpeg.exe NOT included
)
echo.
pause
