using System.Diagnostics;
using System.Text;
using TextBanner.Models;

namespace TextBanner.Services;

public class WindowMonitor
{
    private readonly Dictionary<uint, string> _nameCache = new();
    private readonly GeneralSettings _general;

    public WindowMonitor(GeneralSettings general)
    {
        _general = general;
    }

    public WindowInfo[] GetWindows()
    {
        var list = new List<WindowInfo>();
        try
        {
            Win32.EnumWindows((h, l) =>
            {
                try
                {
                    if (!Win32.IsWindowVisible(h)) return true;
                    var sb = new StringBuilder(512);
                    Win32.GetWindowText(h, sb, sb.Capacity);
                    var title = sb.ToString();
                    if (string.IsNullOrWhiteSpace(title)) return true;
                    Win32.GetWindowThreadProcessId(h, out uint pid);
                    list.Add(new WindowInfo
                    {
                        Handle = h,
                        Title = title,
                        ProcessId = pid,
                        ProcessName = GetProcessName(pid)
                    });
                }
                catch { /* 单个窗口失败不影响整体 */ }
                return true;
            }, IntPtr.Zero);
        }
        catch { }
        return list.ToArray();
    }

    private string GetProcessName(uint pid)
    {
        if (_nameCache.TryGetValue(pid, out var n)) return n;
        try
        {
            n = Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
            n = "";
        }
        _nameCache[pid] = n;
        return n;
    }

    public bool IsBrowser(WindowInfo w) => IsBrowser(w.ProcessName);

    public bool IsBrowser(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        var p = processName.ToLowerInvariant().Replace(".exe", "");
        foreach (var b in _general.BrowserProcesses.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (p == b.ToLowerInvariant().Replace(".exe", "")) return true;
        }
        return false;
    }
}
