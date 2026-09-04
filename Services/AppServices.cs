using System.Windows;
using TextBanner.Models;
using TextBanner.UI;

namespace TextBanner.Services;

public class AppServices : IDisposable
{
    public ConfigService Config { get; }
    public EventLogService EventLog { get; }
    public WindowMonitor WindowMonitor { get; }
    public BrowserTabService BrowserTabs { get; }
    public TabBridgeServer Bridge { get; }
    public FileMonitorService Files { get; }
    public FolderMonitor Folders { get; }
    public BannerService Banner { get; }
    public RuleEngine Engine { get; }
    public TrayIconService Tray { get; private set; }

    private MainWindow _settingsWindow;
    private readonly bool _startMinimized;

    public AppServices(bool startMinimized)
    {
        _startMinimized = startMinimized;
        Config = new ConfigService();
        EventLog = new EventLogService();
        WindowMonitor = new WindowMonitor(Config.General);
        Bridge = new TabBridgeServer();
        BrowserTabs = new BrowserTabService(Config.General, Bridge);
        Files = new FileMonitorService();
        Folders = new FolderMonitor();
        Banner = new BannerService(this, Config.General);
        Engine = new RuleEngine(this);
    }

    public void Start()
    {
        Tray = new TrayIconService(this);
        Bridge.Start();
        Engine.Start();
        if (!_startMinimized)
            OpenSettings();
    }

    public void OpenSettings(string selectRuleId = null, bool eventsTab = false)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new MainWindow(this);
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow.Show();
            }
            else
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                    _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
                _settingsWindow.Topmost = true;
                _settingsWindow.Topmost = false;
            }

            if (selectRuleId != null) _settingsWindow.SelectRule(selectRuleId);
            if (eventsTab) _settingsWindow.ShowEventsTab();
        });
    }

    public void ShowTest()
        => Banner.Show("**加粗** *斜体* **加粗*嵌套斜体***\n\n试试 `代码` 与[链接](https://example.com)", 6, "medium", "测试", "#2F6FED");

    public void TogglePause()
    {
        Config.General.Paused = !Config.General.Paused;
        Config.Save();
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (rk == null) return;
            var exe = Environment.ProcessPath;
            if (enable) rk.SetValue("TextBanner", $"\"{exe}\" --minimized");
            else rk.DeleteValue("TextBanner", false);
        }
        catch { }
    }

    public void Exit()
        => System.Windows.Application.Current?.Shutdown();

    public void Dispose()
    {
        try { Engine?.Stop(); } catch { }
        try { Bridge?.Dispose(); } catch { }
        try { Tray?.Dispose(); } catch { }
        try { Config.Save(); } catch { }
    }
}
