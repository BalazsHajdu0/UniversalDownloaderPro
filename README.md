# Universal Downloader Pro v4.0 - WPF Edition

## 📋 Projekt leírás

Ez a Universal Downloader Pro WPF (Windows Presentation Foundation) verziója, ami .NET Framework 4.8-at használ. A Kivy-vel ellentétben ez:
- **Nem igényel Python telepítést**
- **Nem igényel pip/Kivy/SDL2 telepítést**
- **Egy kattintással telepíthető**
- **.NET Framework 4.8 már telepítve van** minden modern Windows-on

## 🚀 Build utasítások

### 1. Projekt megnyitása
1. Nyisd meg a `UniversalDownloaderWPF.sln` fájlt Visual Studio 2022/2026-ban
2. Várj amíg betöltődik a projekt

### 2. Build
1. Állítsd át **Release** módra (Debug helyett)
2. Build menü → Build Solution (Ctrl+Shift+B)
3. Az exe megjelenik: `UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe`

### 3. Tools mappa előkészítése
A programnak szüksége van ezekre a fájlokra:
```
bin\Release\
├── UniversalDownloader.exe
├── UniversalDownloader.exe.config
└── Tools\
    ├── yt-dlp.exe      ← Töltsd le: https://github.com/yt-dlp/yt-dlp/releases
    └── ffmpeg.exe      ← Töltsd le: https://www.gyan.dev/ffmpeg/builds/
```

#### yt-dlp letöltése:
```
https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe
```

#### ffmpeg letöltése:
1. Menj ide: https://www.gyan.dev/ffmpeg/builds/
2. Töltsd le: `ffmpeg-release-essentials.zip`
3. Csomagold ki
4. Másold be az `ffmpeg.exe` fájlt a `Tools` mappába

## 📦 Installer készítése

### 1. Inno Setup telepítése
Töltsd le és telepítsd: https://jrsoftware.org/isdl.php

### 2. Tools mappa előkészítése
```
Installer\
├── setup.iss
├── LICENSE.txt
└── Tools\
    ├── yt-dlp.exe
    └── ffmpeg.exe
```

### 3. Installer compile
1. Nyisd meg az `Installer\setup.iss` fájlt Inno Setup-pal
2. Build → Compile (Ctrl+F9)
3. A telepítő megjelenik: `Installer\Output\UniversalDownloaderSetup.exe`

## 📁 Projekt struktúra

```
UniversalDownloaderWPF/
├── UniversalDownloaderWPF.sln          # Visual Studio Solution
├── UniversalDownloaderWPF/
│   ├── UniversalDownloaderWPF.csproj   # Projekt fájl
│   ├── App.xaml                        # Alkalmazás + stílusok
│   ├── App.xaml.cs
│   ├── MainWindow.xaml                 # Fő ablak UI
│   ├── MainWindow.xaml.cs
│   ├── Models/
│   │   ├── AppConfig.cs                # Beállítások
│   │   ├── DownloadTask.cs             # Letöltési feladat
│   │   └── PlaylistData.cs             # Beépített playlist
│   ├── Services/
│   │   └── DownloadManager.cs          # Letöltés kezelő
│   └── Properties/
│       └── AssemblyInfo.cs
└── Installer/
    ├── setup.iss                       # Inno Setup script
    ├── LICENSE.txt
    └── Tools/                          # Ide másold a yt-dlp.exe és ffmpeg.exe
```

## ✨ Funkciók

- 🎵 YouTube videók és audio letöltése
- 🔍 Keresés címmel (nem csak URL)
- 📋 Beépített 47 számos playlist
- ⚡ Párhuzamos letöltések (1-5)
- 🔄 Automatikus újrapróbálkozás
- 🎨 Modern sötét téma
- 💾 Több formátum: MP3, WAV, OGG, M4A, MP4
- 🎚️ Minőség választás: 128-320 kbps
- 🏷️ Opcionális ID3 metadata
- 🖼️ Opcionális album borító

## ⚙️ Rendszerkövetelmények

- Windows 10/11 (64-bit)
- .NET Framework 4.8 (már telepítve van)
- ~100 MB szabad hely

## 🔧 Hibakeresés

### "yt-dlp.exe not found"
Másold a `yt-dlp.exe` fájlt a `Tools` mappába.

### "ffmpeg.exe not found"  
Másold az `ffmpeg.exe` fájlt a `Tools` mappába.

### Letöltés nem indul
Ellenőrizd a `downloader.log` fájlt a program mappájában.

## 📝 License

MIT License - Szabad felhasználás

---
Készítette: Universal Downloader Team
