# AUREON YouTube

<img width="1033" height="635" alt="image" src="https://github.com/user-attachments/assets/985bc567-889f-43f8-b316-2a4ea48462b3" />


**AUREON YouTube** is a desktop application for Windows that provides convenient access to YouTube through a modern interface built with C# and Windows Forms.

The application combines YouTube browsing, video playback, a floating mini-player, voice search, and integrated media downloading capabilities in one desktop environment.

Designed with simplicity, functionality, and user convenience in mind, AUREON YouTube delivers an enhanced desktop experience without the need to keep a separate browser window open.

## ✨ Features

- **YouTube Integration** — access YouTube directly through the built-in WebView2 browser engine.
- **Video Playback** — watch YouTube videos within the application.
- **Mini Player (PiP)** — switch to a compact floating window while watching videos.
- **Fullscreen Mode** — immersive video playback with automatic interface adaptation.
- **Voice Search** — use microphone input to search for YouTube content.
- **Video Downloading** — download individual videos in different quality options:
  - 1080p
  - 720p
  - 480p
  - 360p
  - Best available quality
- **Playlist Downloading** — download YouTube playlists.
- **MP3 Extraction** — extract audio and save it in MP3 format.
- **Download Management** — choose download locations, monitor progress, and cancel downloads.
- **SponsorBlock Integration** — optional removal of supported sponsored segments during media processing.
- **Automatic yt-dlp Version Check** — check for available updates to the downloading component.
- **Persistent Settings** — remember window size, position, playback URL, download folder, and preferred quality.
- **Keyboard Shortcuts** — convenient controls for navigation and application functions.
- **System Tray Integration** — access the application through the Windows notification area.

## 🛠️ Technologies

- **C#**
- **.NET Windows Forms**
- **Microsoft Edge WebView2**
- **yt-dlp** — media downloading.
- **FFmpeg** — media processing and audio/video merging.
- **System.Text.Json** — application settings and configuration.
- **Windows API (P/Invoke)** — custom window management and resizing.

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + L` | Open search |
| `F5` / `Ctrl + R` | Refresh YouTube |
| `Alt + ←` | Go back |
| `Alt + →` | Go forward |
| `Alt + Home` | Open YouTube homepage |
| `Ctrl + P` | Toggle mini-player |
| `Esc` | Exit mini-player |
| `Ctrl + D` | Download current video |
| `Ctrl + Shift + D` | Download playlist |
| `Ctrl + Shift + O` | Open downloads folder |
| `Ctrl + Shift + C` | Cancel download |

## 📦 Requirements

- Windows 10 or Windows 11.
- Microsoft Edge WebView2 Runtime.
- yt-dlp executable.
- FFmpeg (recommended for high-quality video processing and audio/video merging).

## ⚙️ Configuration

Application settings are stored in `settings.json`.

The application automatically saves:
- Window position and dimensions.
- Last opened YouTube page.
- Mini-player dimensions and position.
- Download directory.
- Preferred download quality.
- SponsorBlock preference.

## 🚀 Project

AUREON YouTube is part of the AUREON software ecosystem — a collection of desktop applications developed with a focus on functionality, usability, and consistent visual identity.

## ⚖️ Disclaimer

AUREON YouTube is an independent third-party desktop application and is not affiliated with, endorsed by, or officially associated with YouTube or Google.

Users are responsible for complying with applicable copyright laws, YouTube's Terms of Service, and the rights of content owners when downloading or processing media.

---

**AUREON — Technology. Simplicity. Control.**
