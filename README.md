# TextBanner

Text-triggered floating banner for Windows. Built with .NET 8 + WPF, no third-party dependencies.

文本触发的 Windows 悬浮提示条。基于 .NET 8 + WPF，无第三方依赖。

> Watches for keywords in **window titles / browser tab titles / text file contents / opened folder paths**, and pops up a floating banner (optionally opens a URL / folder / file). It does **not** read web page content.
>
> 监视 **窗口标题 / 浏览器标签标题 / 文本文件内容 / 已打开文件夹路径** 中出现的关键词，弹出一条悬浮提示（可选同时打开网址 / 文件夹 / 文件）。它**不会**读取网页正文内容。

## Install · 安装

### Option A · 便携版（免安装）

Download `release-win-x64.zip` from [Releases](../../releases), unzip, run `TextBanner.exe`.
从 [Releases](../../releases) 下载 `release-win-x64.zip`，解压后运行 `TextBanner.exe`。

### Option B · 安装器（当前用户，无需管理员）

```powershell
.\publish.ps1 -SelfContained   # build a self-contained bundle into publish\
.\install.ps1                  # install: copies files, creates Start Menu + Desktop shortcuts, registers uninstaller
```

- Installs to `%LocalAppData%\Programs\TextBanner` · 安装到 `%LocalAppData%\Programs\TextBanner`
- Uninstall: run `uninstall.ps1` in that folder, or via *Settings → Apps* · 卸载：运行安装目录下的 `uninstall.ps1`，或在「设置 → 应用」里卸载

> **Setup.exe (optional)**：install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then `ISCC.exe installer\TextBanner.iss` to produce `dist\TextBanner-Setup.exe`.
> **可选 Setup.exe**：安装 [Inno Setup 6](https://jrsoftware.org/isinfo.php) 后运行 `ISCC.exe installer\TextBanner.iss`，生成 `dist\TextBanner-Setup.exe`。

## Set language · 设置语言

- Settings → **General** → **Appearance** → **Language** → 中文 / English（applies instantly）
- 设置 → **常规设置** → **外观** → **语言** → 中文 / English（即时生效）

## Browser tab detection · 浏览器标签检测

The *browser tab* source fires only for **newly opened** tabs. Enable one of:

1. **Companion extension (recommended)** — load `browser-extension/` as an unpacked extension:
   - Chrome `chrome://extensions` → Developer mode → Load unpacked · Edge `edge://extensions` → 开发人员模式 → 加载解压缩的扩展
2. **CDP debug port** — launch the browser with `--remote-debugging-port=9222` (Chrome) / `9223` (Edge), after fully quitting it first.

「浏览器标签」来源只在**新打开标签页**时触发。方式一：加载 `browser-extension/` 为解压扩展（推荐，免重启浏览器）；方式二：给浏览器加 `--remote-debugging-port` 启动参数（先完全退出浏览器）。

## Features · 功能

- 4 trigger sources: browser tab / folder / window title / text file（4 种来源：浏览器标签 / 文件夹 / 窗口标题 / 文本文件）
- Floating banner: double-click = settings, right-click/✕ = close, long-press = pin（悬浮条：双击设置、右键/✕ 关闭、长按固定）
- Markdown display text（显示文本支持 Markdown，样式可叠加）
- Themes: Light/Dark/Mint/Dusk/Crimson + follow system（5 套主题 + 跟随系统）
- Per-rule color + "open on trigger" action（每条规则自定义颜色 + 触发后打开动作）

## Screenshots

| Settings 设置 | Banner 提示条 | Themes 主题 |
|---|---|---|
| ![settings](docs/screenshots/settings.png) | ![banner](docs/screenshots/banner.png) | ![themes](docs/screenshots/themes.png) |

## Build from source · 从源码构建

```powershell
.\publish.ps1                     # framework-dependent
.\publish.ps1 -SelfContained      # self-contained (no .NET runtime needed)
.\publish.ps1 -Zip                # also package a release .zip
```

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## License

[MIT](LICENSE)
