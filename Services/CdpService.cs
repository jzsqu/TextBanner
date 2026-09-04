using System.Net.Http;
using System.Text.Json;
using TextBanner.Models;
using TextBanner.UI;

namespace TextBanner.Services;

/// <summary>
/// 通过 Chrome DevTools Protocol（CDP）的 HTTP 端点读取浏览器所有标签（含后台标签）与精确 URL。
/// 前提：浏览器以 --remote-debugging-port=&lt;端口&gt; 启动。
/// </summary>
public class CdpService
{
    private readonly GeneralSettings _general;
    private readonly HttpClient _http;
    private readonly Dictionary<int, DateTime> _deadUntil = new();
    private readonly Dictionary<int, string> _portBrowser = new();

    public CdpService(GeneralSettings general)
    {
        _general = general;
        var handler = new HttpClientHandler { UseProxy = false }; // 只连 127.0.0.1，禁用代理避免被拦截/超时
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(600) };
    }

    /// <summary>返回 (所有 CDP 标签, 已成功覆盖的浏览器进程名集合)。</summary>
    public async Task<(List<TabInfo> Tabs, HashSet<string> Covered)> GetTabsAsync()
    {
        var tabs = new List<TabInfo>();
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var port in SplitPorts(_general.CdpPorts))
        {
            // 端口此前连接失败则短暂跳过，避免反复空连接
            if (_deadUntil.TryGetValue(port, out var dead) && DateTime.UtcNow < dead)
                continue;

            try
            {
                var browser = await GetBrowserNameAsync(port);
                if (browser == null)
                {
                    MarkDead(port);
                    continue;
                }

                var list = await GetTargetsAsync(port, browser);
                if (list != null)
                {
                    tabs.AddRange(list);
                    covered.Add(browser);
                }
                _portBrowser[port] = browser;
                _deadUntil.Remove(port);
            }
            catch
            {
                MarkDead(port);
            }
        }
        return (tabs, covered);
    }

    private void MarkDead(int port) => _deadUntil[port] = DateTime.UtcNow.AddSeconds(5);

    public string GetStatusDescription()
    {
        if (string.IsNullOrWhiteSpace(_general.CdpPorts))
            return Loc.Get("CdpPrefix") + Loc.Get("CdpNotConfigured");
        var parts = new List<string>();
        foreach (var port in SplitPorts(_general.CdpPorts))
        {
            bool connected = _portBrowser.TryGetValue(port, out var b)
                && (!_deadUntil.TryGetValue(port, out var d) || DateTime.UtcNow >= d);
            parts.Add(connected ? $"{b}({port}) {Loc.Get("CdpPortConnected")}" : $"{port} {Loc.Get("CdpPortNotConnected")}");
        }
        return Loc.Get("CdpPrefix") + string.Join(" · ", parts);
    }

    private async Task<string> GetBrowserNameAsync(int port)
    {
        var json = await _http.GetStringAsync($"http://127.0.0.1:{port}/json/version");
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("Browser", out var b))
            return MapBrowser(b.GetString() ?? "");
        return null;
    }

    private async Task<List<TabInfo>> GetTargetsAsync(int port, string browser)
    {
        var json = await _http.GetStringAsync($"http://127.0.0.1:{port}/json/list");
        var list = new List<TabInfo>();
        using var doc = JsonDocument.Parse(json);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("type", out var t) && t.GetString() != "page") continue;
            string id = el.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? "") : "";
            string title = el.TryGetProperty("title", out var ti) ? (ti.GetString() ?? "") : "";
            string url = el.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "";
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(url)) continue;
            list.Add(new TabInfo { Title = title, Url = url, Browser = browser, Id = id });
        }
        return list;
    }

    private static string MapBrowser(string s)
    {
        if (s == null) return "chrome";
        if (s.Contains("Edg", StringComparison.OrdinalIgnoreCase)) return "msedge";
        if (s.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) return "chrome";
        if (s.Contains("Brave", StringComparison.OrdinalIgnoreCase)) return "brave";
        if (s.Contains("Vivaldi", StringComparison.OrdinalIgnoreCase)) return "vivaldi";
        if (s.Contains("Opera", StringComparison.OrdinalIgnoreCase) || s.Contains("OPR", StringComparison.OrdinalIgnoreCase)) return "opera";
        if (s.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) return "firefox";
        return "chrome";
    }

    private static IEnumerable<int> SplitPorts(string s)
    {
        foreach (var p in (s ?? "").Split(new[] { ';', '|', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(p, out int port) && port > 0 && port < 65536)
                yield return port;
        }
    }
}
