using System.IO;

namespace TextBanner.Services;

/// <summary>
/// 枚举当前已打开的资源管理器（Explorer）窗口，返回它们的完整文件夹路径。
/// 通过 Shell.Application COM（IShellWindows）获取 LocationURL，比窗口标题更精确（含完整路径）。
/// </summary>
public class FolderMonitor
{
    private readonly object _lock = new();
    private DateTime _lastCheck = DateTime.MinValue;
    private List<string> _cache = new();

    public List<string> GetOpenFolders()
    {
        lock (_lock)
        {
            if ((DateTime.UtcNow - _lastCheck).TotalMilliseconds < 1500)
                return new List<string>(_cache);
        }

        var folders = RunSta(Enumerate) ?? new List<string>();

        lock (_lock)
        {
            _cache = folders;
            _lastCheck = DateTime.UtcNow;
            return new List<string>(_cache);
        }
    }

    private static List<string> Enumerate()
    {
        var result = new List<string>();
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return result;

            dynamic shell = Activator.CreateInstance(shellType);
            foreach (dynamic win in shell.Windows())
            {
                try
                {
                    string url = win.LocationURL as string;
                    if (string.IsNullOrEmpty(url) || !url.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string path = Uri.UnescapeDataString(new Uri(url).LocalPath);
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    try { path = Path.GetFullPath(path); } catch { continue; }
                    if (!Directory.Exists(path)) continue;

                    if (!result.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
                        result.Add(path);
                }
                catch { /* 跳过不可访问的窗口 */ }
            }
        }
        catch { }
        return result;
    }

    private static List<string> RunSta(Func<List<string>> func)
    {
        List<string> result = null;
        var t = new Thread(() => { try { result = func(); } catch { } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join(1500);
        return result;
    }
}
