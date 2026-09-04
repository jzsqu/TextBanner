namespace TextBanner.UI;

/// <summary>轻量本地化：根据当前语言返回中文或英文字符串。</summary>
public static class Loc
{
    public static string Lang = "zh"; // zh | en

    private static readonly Dictionary<string, (string Zh, string En)> T = new()
    {
        // 应用 / 标题
        ["SettingsTitle"] = ("文本触发提醒 — 设置", "TextBanner — Settings"),
        ["AppTitle"] = ("文本触发提醒", "TextBanner"),
        ["AppSubtitle"] = ("悬浮提示栏 · 基于文本触发", "Floating banner · text-triggered"),

        // 标签页
        ["TabRules"] = ("规则", "Rules"),
        ["TabEvents"] = ("事件记录", "Events"),
        ["TabGeneral"] = ("常规设置", "General"),

        // 分节
        ["SecBasic"] = ("基本信息", "Basic info"),
        ["SecTrigger"] = ("触发条件", "Trigger"),
        ["SecDisplay"] = ("显示效果", "Display"),
        ["SecAppearance"] = ("外观", "Appearance"),
        ["SecBehavior"] = ("行为", "Behavior"),
        ["SecBrowser"] = ("浏览器检测", "Browser detection"),
        ["SecSystem"] = ("系统", "System"),

        // 标签
        ["LblName"] = ("规则名称", "Name"),
        ["LblSource"] = ("触发来源", "Source"),
        ["LblMatchTarget"] = ("匹配目标", "Match target"),
        ["LblSourceFilter"] = ("来源限定", "Source filter"),
        ["LblDisplayText"] = ("显示文本（支持 Markdown，可叠加样式）", "Display text (Markdown, combinable)"),
        ["LblAction"] = ("触发后打开（可选）", "Open on trigger (optional)"),
        ["LblDuration"] = ("显示时长（秒）", "Duration (seconds)"),
        ["LblSize"] = ("显示大小", "Size"),
        ["LblColor"] = ("提示条颜色", "Banner color"),
        ["LblTheme"] = ("主题", "Theme"),
        ["LblLanguage"] = ("语言", "Language"),
        ["LblPosition"] = ("提示栏位置", "Banner position"),
        ["LblDefaultDuration"] = ("默认显示时长（秒）", "Default duration (seconds)"),
        ["LblPoll"] = ("检测间隔（毫秒）", "Poll interval (ms)"),
        ["LblCooldown"] = ("触发冷却（秒）", "Cooldown (seconds)"),
        ["LblBrowserProc"] = ("浏览器进程名", "Browser processes"),
        ["LblCdp"] = ("CDP 调试端口", "CDP debug port"),

        // 复选框 / 按钮
        ["ChkEnabled"] = ("启用此规则", "Enable this rule"),
        ["ChkAutoStart"] = ("开机自动启动（登录后最小化到托盘）", "Start with Windows (minimized to tray)"),
        ["ChkPaused"] = ("暂停所有监控", "Pause all monitoring"),
        ["BtnNew"] = ("＋ 新建", "＋ New"),
        ["BtnDuplicate"] = ("复制", "Duplicate"),
        ["BtnDelete"] = ("删除", "Delete"),
        ["BtnTest"] = ("▶ 测试这条提示", "▶ Test this rule"),
        ["BtnClear"] = ("清空记录", "Clear"),

        // 提示
        ["HintMatch"] = ("例如：关键词，或多个用 | 或 ; 分隔（任一命中即触发，不区分大小写）", "e.g. keyword, or several separated by | or ; (any match, case-insensitive)"),
        ["HintMarkdown"] = ("支持 **加粗** *斜体* `代码` ~~删除线~~ [链接](url)、# 标题、- 列表", "Supports **bold** *italic* `code` ~~strike~~ [link](url), # heading, - list"),
        ["HintAction"] = ("触发时同时打开一个或多个网址 / 文件夹 / 文件（如 https://…、D:\\logs、D:\\a.txt），多个用 ; 分隔，留空则不打开", "Also open one or more URLs / folders / files on trigger (e.g. https://…, D:\\logs, D:\\a.txt); separate with ;"),
        ["HintTheme"] = ("明亮 / 夜间 / 薄荷 / 暮紫 / 绯红，切换后设置界面与提示条即时换肤", "Light / Dark / Mint / Dusk / Crimson; applies instantly"),
        ["HintPosition"] = ("悬浮提示栏出现在屏幕的位置，多条提示会依次堆叠", "Where banners appear; multiple banners stack"),
        ["HintCooldown"] = ("同一条规则两次触发之间的最短间隔，避免反复弹窗", "Minimum gap between two triggers of the same rule"),
        ["HintBrowserProc"] = ("用于识别“浏览器标签”来源，多个进程名用 ; 分隔，例如 chrome;msedge;firefox", "Process names for the browser-tab source; separate with ;"),
        ["HintCdp"] = ("浏览器需以 --remote-debugging-port 启动（如 Chrome=9222、Edge=9223）。本程序据此在『新打开标签页』时触发一次。多个端口用 ; 分隔", "Launch the browser with --remote-debugging-port (Chrome=9222, Edge=9223). Used for new-tab detection. Separate ports with ;"),
        ["HintEvents"] = ("最多保留 500 条，新的在最上方", "Keeps the latest 500, newest first"),
        ["HintAutoSave"] = ("配置自动保存。配置文件位置：%APPDATA%\\TextBanner\\config.json", "Config auto-saves. Location: %APPDATA%\\TextBanner\\config.json"),

        // 匹配文本标签（按来源）
        ["MatchLabelBrowser"] = ("匹配文本（新标签页的标题 / 地址中包含）", "Match text (in new tab title / URL)"),
        ["MatchLabelWindow"] = ("匹配文本（窗口标题中包含）", "Match text (in window title)"),
        ["MatchLabelFile"] = ("匹配文本（文件内容中包含）", "Match text (in file content)"),
        ["MatchLabelFolder"] = ("匹配文本（打开的文件夹路径中包含）", "Match text (in opened folder path)"),

        // 来源限定提示（按来源）
        ["FilterHintBrowser"] = ("来源限定：仅匹配这些浏览器进程名（留空 = 所有浏览器），例如 chrome;msedge。仅在『新打开标签页』时触发一次", "Filter: only these browser process names (empty = all). Fires once per newly opened tab"),
        ["FilterHintWindow"] = ("来源限定：窗口标题还需包含这些文字（留空 = 任意窗口），例如 某个窗口/应用名", "Filter: window title must also contain this (empty = any window)"),
        ["FilterHintFile"] = ("来源限定：要监视的文件 / 文件夹 / 通配符，例如 D:\\logs\\*.txt（文件夹会监视其中常见文本文件）", "Filter: file / folder / wildcard to watch, e.g. D:\\logs\\*.txt"),
        ["FilterHintFolder"] = ("例如 D:\\某个文件夹 或 文件夹名；在该文件夹于资源管理器中被打开时触发一次（来源限定可留空）", "e.g. D:\\some-folder or a folder name; fires once when opened in Explorer"),

        // 下拉选项
        ["SrcBrowser"] = ("浏览器标签", "Browser tab"),
        ["SrcWindow"] = ("窗口名字", "Window title"),
        ["SrcFile"] = ("文本文件", "Text file"),
        ["SrcFolder"] = ("文件夹", "Folder"),
        ["TargetTitle"] = ("标签标题", "Tab title"),
        ["TargetUrl"] = ("标签地址 URL", "Tab URL"),
        ["SizeSmall"] = ("小", "Small"),
        ["SizeMedium"] = ("中", "Medium"),
        ["SizeLarge"] = ("大", "Large"),
        ["PosTopRight"] = ("右上角", "Top-right"),
        ["PosBottomRight"] = ("右下角", "Bottom-right"),
        ["PosTopLeft"] = ("左上角", "Top-left"),
        ["PosBottomLeft"] = ("左下角", "Bottom-left"),
        ["Poll300"] = ("300 毫秒（灵敏）", "300 ms (fast)"),
        ["Poll600"] = ("600 毫秒（默认）", "600 ms (default)"),
        ["Poll1000"] = ("1000 毫秒", "1000 ms"),
        ["Poll2000"] = ("2000 毫秒（省电）", "2000 ms (battery)"),
        ["ThemeAuto"] = ("跟随系统", "System"),
        ["ThemeLight"] = ("明亮", "Light"),
        ["ThemeDark"] = ("夜间", "Dark"),
        ["ThemeMint"] = ("薄荷", "Mint"),
        ["ThemeDusk"] = ("暮紫", "Dusk"),
        ["ThemeCrimson"] = ("绯红", "Crimson"),
        ["ColorTheme"] = ("跟随主题", "Follow theme"),
        ["ColorBlue"] = ("蓝色", "Blue"),
        ["ColorRed"] = ("红色", "Red"),
        ["ColorGreen"] = ("绿色", "Green"),
        ["ColorOrange"] = ("橙色", "Orange"),
        ["ColorPurple"] = ("紫色", "Purple"),
        ["ColorCyan"] = ("青色", "Cyan"),
        ["ColorPink"] = ("粉色", "Pink"),
        ["ColorGray"] = ("灰色", "Gray"),

        // 横幅
        ["BannerTitle"] = ("触发提示", "Trigger alert"),
        ["BannerHint"] = ("双击设置 · 右键关闭 · 长按固定", "Double-click settings · right-click close · long-press pin"),
        ["BannerPinned"] = ("已固定 · 右键或 ✕ 关闭 · 长按取消固定", "Pinned · right-click/✕ close · long-press unpin"),

        // 托盘
        ["TrayOpenSettings"] = ("打开设置", "Open settings"),
        ["TrayRules"] = ("规则列表", "Rules"),
        ["TrayEvents"] = ("事件记录", "Events"),
        ["TrayPause"] = ("暂停监控", "Pause monitoring"),
        ["TrayResume"] = ("恢复监控", "Resume monitoring"),
        ["TrayTest"] = ("测试提示", "Test banner"),
        ["TrayExit"] = ("退出", "Exit"),

        // 来源标签
        ["SrcLabelBrowser"] = ("浏览器标签", "Browser tab"),
        ["SrcLabelWindow"] = ("窗口名字", "Window title"),
        ["SrcLabelFile"] = ("文本文件", "Text file"),
        ["SrcLabelFolder"] = ("文件夹", "Folder"),

        // 其它
        ["MsgRunning"] = ("TextBanner 已在运行，请查看系统托盘图标。", "TextBanner is already running. Check the system tray."),
        ["MsgStartupFailed"] = ("启动失败：", "Startup failed: "),
        ["EngineError"] = ("内部错误", "Internal error"),
        ["EngineSource"] = ("引擎", "Engine"),
        ["TestTitle"] = ("测试", "Test"),
        ["TestContent"] = ("**加粗** *斜体* **加粗*嵌套斜体***\n\n试试 `代码` 与[链接](https://example.com)", "**bold** *italic* **bold *nested italic***\n\nTry `code` and a [link](https://example.com)"),
        ["NoRules"] = ("（无规则）", "(no rules)"),
        ["Unnamed"] = ("（未命名）", "(unnamed)"),

        // 事件列表列头
        ["ColTime"] = ("时间", "Time"),
        ["ColRule"] = ("规则", "Rule"),
        ["ColSource"] = ("来源", "Source"),
        ["ColMatch"] = ("匹配内容", "Matched"),

        // 格式工具栏
        ["TipBold"] = ("加粗 **文字**", "Bold **text**"),
        ["TipItalic"] = ("斜体 *文字*", "Italic *text*"),
        ["TipStrike"] = ("删除线 ~~文字~~", "Strike ~~text~~"),
        ["TipCode"] = ("行内代码 `文字`", "Inline code `text`"),
        ["TipLink"] = ("链接 [文字](https://)", "Link [text](https://)"),
        ["TipHeading"] = ("标题 # ", "Heading # "),
        ["TipList"] = ("列表 - ", "List - "),
        ["SecUnit"] = ("秒", "s"),

        // 规则操作
        ["AddRuleName"] = ("新规则", "New rule"),
        ["AddRuleDisplay"] = ("这是新的触发提示内容", "New trigger message"),
        ["DupSuffix"] = ("（副本）", " (copy)"),
        ["DelTitle"] = ("删除规则", "Delete rule"),
        ["DelConfirm"] = ("确定删除规则“{0}”吗？", "Delete rule \"{0}\"?"),
        ["FmtText"] = ("文字", "text"),
        ["FmtCode"] = ("代码", "code"),
        ["FmtLinkText"] = ("链接文字", "link text"),

        // 状态行
        ["MatchPrefix"] = ("匹配：", "Match: "),
        ["ExtConnected"] = ("扩展已连接：", "Extension connected: "),
        ["ExtNotConnected"] = ("扩展未连接", "Extension not connected"),
        ["TabCount"] = ("个标签", "tabs"),
        ["CdpPrefix"] = ("浏览器标签检测（CDP）：", "Browser tab detection (CDP): "),
        ["CdpNotConfigured"] = ("未配置调试端口", "no debug port configured"),
        ["CdpPortConnected"] = ("已连接", "connected"),
        ["CdpPortNotConnected"] = ("未连接", "not connected"),
    };

    public static string Get(string key)
        => T.TryGetValue(key, out var v) ? (Lang == "en" ? v.En : v.Zh) : key;
}
