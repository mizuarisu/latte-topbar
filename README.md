# TopBar

A Caelestia-style popup panel for Windows. Pure WPF + raw Win32 P/Invoke —
**zero WebView2, zero Electron, zero Chromium runtime.**

Press your hotkey (default **Alt + Q**, changeable in Settings) anywhere on
your desktop to pop the panel up top-center; press it again, click away, or
lose focus to close it. It runs from the tray — right-click the tray icon
for **Settings** or **Exit**.

## Tabs

- **Dashboard** — profile picture, clock, date.
- **Media** — current now-playing (title/artist) from the native Windows
  media session API, same source the volume flyout reads from. If the
  active session is Spotify, it also looks up lyrics via
  [LRCLIB](https://lrclib.net) (free, no API key) and shows them alongside.
- **Performance** — live CPU / RAM / disk / network numbers, task-manager
  style but summary-level (no per-process list).
- **Weather** — current temp + condition for a location you set in Settings.

## Settings

Opened from the tray icon → Settings.

- **Hotkey** — click the box, press a key, done. Alt is the fixed modifier;
  the key part is whatever you choose (default Q).
- **Launch at startup** — toggles a per-user Run registry entry, no
  installer or scheduled task needed.
- **Theme** — three hex color fields (Main/Secondary/Accent) applied live
  across the panel.
- **Profile picture** — any local image file, shown on the Dashboard tab.
- **Weather location** — label + lat/lon, plus a °C/°F toggle.

Settings persist to `%AppData%\TopBar\settings.json`.

## Build a standalone exe with no local install (recommended)

This repo includes `.github/workflows/build.yml`, which builds a fully
self-contained `TopBar.exe` on GitHub's own Windows runners — no .NET SDK,
no Visual Studio, nothing installed on your machine.

1. Push/upload this folder (including the hidden `.github` folder) to a
   GitHub repo via the web UI. If the uploader hides dotfiles, use GitHub's
   web-based file editor to create `.github/workflows/build.yml` directly
   and paste its contents in.
2. Go to the **Actions** tab — a run starts automatically (or click
   **Run workflow**).
3. Once green, open the run → **Artifacts** → download `TopBar-windows-x64`.
   That zip contains `TopBar.exe` — self-contained, no .NET runtime needed
   on the machine you run it on.

Rerun the workflow (or just push a change) any time you edit something, and
grab the new exe the same way.

## Build & run locally instead

Requires the .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`):

```
cd TopBar
dotnet build
dotnet run
```

Publish the same standalone single-file exe locally:

```
dotnet publish -c Release -r win-x64 --self-contained true
```

## Architecture notes

- **Hotkey, not hover.** Global hotkey via `RegisterHotKey`/`WM_HOTKEY`
  (`Services/HotkeyService.cs`) — reliable regardless of what window has
  focus, and reconfigurable at runtime without a restart.
- **No AppBar docking anymore.** Earlier versions of this project reserved
  a persistent strip like the taskbar; the panel model doesn't need that
  since it's hidden until summoned.
- **Tray icon** exists because there's no persistent visible surface to
  right-click otherwise — it's the only way to reach Settings/Exit.
- **Workspace/virtual-desktop indicator and system tray hosting** are still
  intentionally out of scope — Windows has no stable public API for either;
  see git history / earlier notes if you want that tradeoff explained.

## Extending it

Each data source is a self-contained `Services/*Service.cs`. To add a
widget: write a service, poll or event-hook it from `PanelWindow.xaml.cs`,
add the UI to the relevant tab in `PanelWindow.xaml`. No HTML/CSS/JS
anywhere in this project.
