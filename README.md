# TextBanner

A lightweight Windows tray app that pops up a **floating banner** whenever text you care about appears in one of:

- a **window title**
- a **browser tab's title** (and URL)
- a **text file's contents**
- an **opened folder's path**

Built with **.NET 8 + WPF** and **no third-party dependencies**.

> **Precision note:** TextBanner only reads the four things listed above. A *browser-tab* rule matches the tab's **title** (or **URL**) — it does **not** parse or read the body/HTML content of web pages. Likewise a *folder* rule matches the folder's **path/name**, and a *file* rule matches the file's **content**, not its metadata.

> 中文说明见 [使用说明.md](使用说明.md)。

## Why?

When you have a workflow or pipeline running in the background, you often want a visible reminder the moment something textual shows up — a tab titled `deploy` opens, a folder named `logs` is opened in Explorer, a log file gains the word `error`, or a window titled `rendering…` appears. TextBanner watches for these and shows a banner, and can optionally open a URL / folder / file for you at the same time.

## Features

### Four trigger sources

| Source | What is matched | Fires when |
|--------|-----------------|------------|
| **Browser tab** | tab title or URL | a *newly opened* tab matches |
| **Folder** | full folder path / name | the folder is opened in File Explorer |
| **Window title** | window title text | any visible window title matches |
| **Text file** | file contents (UTF-8 / GBK auto-detected) | a watched file's content matches |

- Case-insensitive; multiple keywords separated by `|` or `;` (any-of).
- Each rule has its own **display text**, **duration**, **size**, and **color**.
- Optional **"open on trigger"** action — open a URL / folder / file (multiple, `;`-separated).

### Floating banner

Topmost, rounded, draggable, auto-dismiss:

- **double-click** → open settings
- **right-click** or **✕** → close
- **long-press (~0.6 s)** → pin / unpin (a pinned banner does not auto-dismiss)

### Markdown in the banner

`**bold**`, `*italic*`, `` `code` ``, `~~strike~~`, `[link](url)`, `# headings`, `- lists` — styles can be **nested/combined** (e.g. `**bold *italic***`). The settings UI includes a formatting toolbar.

### Themes

Light / Dark / Mint / Dusk / Crimson, plus **follow system**. Three-tone minimalist UI.

### Other

- System tray menu: open settings, rule list, event log, pause, test, exit.
- Event log (last 500 triggers).
- JSON config at `%APPDATA%\TextBanner\config.json` (UTF-8); auto-start.

## Screenshots

> ⚠️ 以下为占位图，请替换成你的截图（图片在 `docs/screenshots/` 目录下）。

| 设置界面 | 悬浮提示条 | 主题 |
|---|---|---|
| ![settings](docs/screenshots/settings.png) | ![banner](docs/screenshots/banner.png) | ![themes](docs/screenshots/themes.png) |

把 `docs/screenshots/` 下的三张 PNG 换成你自己的截图即可。

## Requirements

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — only needed to build.

## Build & run

```powershell
.\publish.ps1                     # framework-dependent
.\publish.ps1 -SelfContained      # self-contained (no .NET runtime needed on target)
.\publish.ps1 -Zip                # also package a release .zip
```

Then run `publish\TextBanner.exe`.

## Browser tab detection

The **browser tab** source fires only for *newly opened* tabs. Enable one (or both) of:

1. **Companion extension (recommended — no browser restart)**
   - Chrome: `chrome://extensions` → Developer mode → "Load unpacked" → select `browser-extension/`.
   - Edge: `edge://extensions` → Developer mode → "Load unpacked" → same folder.
   - The extension pushes all tab titles/URLs to the app over `127.0.0.1:51739`.

2. **CDP debug port**
   - Launch the browser with `--remote-debugging-port=9222` (Chrome) / `9223` (Edge), after fully quitting it first.

The current connection status is shown in *Settings → General → CDP debug port*.

## Project layout

```
TextBanner/
├── Models/             # rule / config / event models
├── Services/           # window / folder / file / browser monitors, rule engine, bridge server
├── UI/                 # settings window, banner, markdown renderer, themes
├── browser-extension/  # companion Chrome/Edge MV3 extension
├── .github/workflows/  # tag-triggered release build
├── publish.ps1         # build / publish script
└── 使用说明.md          # Chinese user guide
```

## Releases

Pushing a `v*` tag (e.g. `v1.0.0`) triggers a GitHub Actions build that produces a self-contained `release-win-x64.zip` and attaches it to a GitHub Release.

## License

[MIT](LICENSE)
