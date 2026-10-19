# Auto Screen Capture & Key Presser 📸

[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

An intelligent, lightweight Windows desktop application written in **C# (.NET 9 Windows Forms)** designed to automate repetitive screen capturing workflows. It allows you to interactively drag and select a custom screen region, automate captures at configurable intervals, trigger custom keystrokes after capture (e.g. advance to next slide/page), auto-sequence filenames, stop at a preset target count with a completion alert, and persist settings in a JSON configuration file.

---

## 🌟 Key Features

### 1. 🖱️ Interactive Screen Region Selection (Drag & Drop)
- Click **"Pilih Area Layar (Drag)"** to activate a semi-transparent screen overlay across single or multi-monitor setups.
- Click and drag across the screen to define the exact capture coordinates and dimensions (with live pixel resolution feedback).
- Right-click or press <kbd>ESC</kbd> to cancel anytime.
- Coordinates and dimensions are **instantly saved to `config.json`**.
- Includes a **"Layar Penuh"** button to toggle back to full desktop capture.

### 2. ⏱️ Configurable Screenshot Interval
- Set capture interval in seconds (e.g., capture every `10` seconds).
- Live countdown timer in the status bar (e.g., `Screenshot #1 [1/100] in 8 seconds...`).

### 3. 🎯 Target Capture Limit & "Job Done" Notification
- Specify the target count of screenshots (e.g. `100`).
- Option to toggle between target-limited mode and continuous/unlimited capture.
- Once the target screenshot is taken:
  - Capturing loop automatically halts.
  - The application window restores and activates to the foreground.
  - System sound is played alongside a **"🎉 Job Done!"** pop-up alert dialog.

### 4. ⌨️ Post-Capture Key Press Action (Key Automator)
- Configure an optional post-capture delay (e.g. `2` seconds after capture).
- Automatically triggers a simulated keystroke via `SendKeys` to navigate the active target application.
- Preset keys supported:
  - `{RIGHT}` (Right Arrow - Next Slide/Page)
  - `{PGDN}` (Page Down)
  - `{ENTER}` (Enter)
  - `{SPACE}` (Spacebar)
  - `{DOWN}` (Down Arrow)
  - `{LEFT}` (Left Arrow)
  - `{TAB}` (Tab)
  - `NONE` (Disable keystrokes)
  - Custom key combinations (e.g., `^s`, `{F5}`, etc.)

### 5. 📁 Automatic Sequential Numbering (`1.png` .. `N.png`)
- Custom output destination folder with interactive folder browser.
- Automatically saves images named sequentially: `1.png`, `2.png`, ... `N.png`.
- **Smart detection**: Automatically resumes from the highest existing file number in the folder to prevent accidental overwrites.
- Quick folder launch button (📂) to view captured images in Windows Explorer.

### 6. 🌐 Global Hotkey (<kbd>F9</kbd>) & Auto-Minimize
- Press <kbd>F9</kbd> globally from any active program to **Start** or **Stop** capture without switching windows.
- Auto-minimize feature minimizes the app to the taskbar upon starting so it doesn't obstruct the capture area.

### 7. 💾 Persistent JSON Configuration (`config.json`)
- All user preferences (screen coordinates, intervals, keypress commands, destination directory, and target limits) are automatically saved to `config.json` and restored on the next startup.

---

## 🛠️ Tech Stack & Requirements

- **Framework**: .NET 9.0 SDK (`net9.0-windows`)
- **Language**: C# 13
- **GUI Engine**: Windows Forms (High-DPI aware, Per-Monitor V2)
- **OS Supported**: Windows 10 / Windows 11 (x64 / ARM64)

---

## ⚙️ Configuration File (`config.json`)

The application automatically creates and loads `config.json`:

```json
{
  "CaptureArea": {
    "X": 100,
    "Y": 150,
    "Width": 1280,
    "Height": 720
  },
  "IsCustomArea": true,
  "ScreenshotIntervalSeconds": 10.0,
  "PostCaptureDelaySeconds": 2.0,
  "KeyToPress": "{RIGHT}",
  "OutputFolder": "D:\\SCREEN_SHOOT\\Screenshots",
  "StartIndex": 1,
  "AutoDetectNextNumber": true,
  "FileNameFormat": "{0}.png",
  "TargetScreenshotCount": 100,
  "UseTargetCount": true,
  "MinimizeOnStart": true
}
```

### Parameter Breakdown:

| Parameter | Type | Default | Description |
| :--- | :---: | :---: | :--- |
| `CaptureArea` | `Object` | Full Screen | Coordinates `(X, Y)` and dimensions `(Width, Height)` of the region. |
| `IsCustomArea` | `Boolean` | `false` | `true` if custom region selected; `false` for full desktop. |
| `ScreenshotIntervalSeconds` | `Number` | `10.0` | Delay between consecutive captures (in seconds). |
| `PostCaptureDelaySeconds` | `Number` | `2.0` | Jitter/delay after screenshot before pressing the key. |
| `KeyToPress` | `String` | `"{RIGHT}"` | Key code to send via `SendKeys` (`{RIGHT}`, `{PGDN}`, `NONE`, etc.). |
| `OutputFolder` | `String` | `"Screenshots"` | Destination directory path for saved images. |
| `StartIndex` | `Integer` | `1` | Starting integer index for output filenames. |
| `AutoDetectNextNumber` | `Boolean` | `true` | Continues numbering from highest existing number in directory. |
| `TargetScreenshotCount` | `Integer` | `100` | Target number of captures before halting and alerting. |
| `UseTargetCount` | `Boolean` | `true` | Toggles target completion limit. |
| `MinimizeOnStart` | `Boolean` | `true` | Minimizes app to taskbar during capture session. |

---

## 🚀 Getting Started

### Prerequisites
Make sure [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) is installed on your Windows machine:
```powershell
dotnet --version
```

### Running from Source
1. Clone or download this repository:
   ```powershell
   git clone https://github.com/your-username/auto-screen-capture.git
   cd auto-screen-capture
   ```
2. Build and run the project:
   ```powershell
   dotnet run
   ```

### Publishing a Standalone Executable
To produce an optimized, standalone Windows binary:
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```
The executable will be located under `bin/Release/net9.0-windows/win-x64/publish/ScreenCaptureApp.exe`.

---

## 📖 Usage Guide

1. **Launch the application**: Run `ScreenCaptureApp.exe` or `dotnet run`.
2. **Select Area**: Click **"Pilih Area Layar (Drag)"**, then drag mouse over the target window/area. Press <kbd>ESC</kbd> if you wish to cancel.
3. **Set Timing**: Set the interval (e.g. `10`s) and post-capture delay (e.g. `2`s).
4. **Choose Key Action**: Choose which key advances your target program (e.g. `{RIGHT}` for slides, `{PGDN}` for PDF/books, or `NONE`).
5. **Set Destination & Target**: Choose output folder and set your target count (e.g. `100`).
6. **Start Capturing**: Click **"Mulai Screenshot"** or press <kbd>F9</kbd>.
7. **Finished**: Once target count is reached, a **"Job Done!"** alert will pop up and the session will conclude cleanly. You can also press <kbd>F9</kbd> at any time to abort early.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE). Feel free to use, modify, and distribute.
