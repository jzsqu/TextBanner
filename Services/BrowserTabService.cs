using TextBanner.Models;

namespace TextBanner.Services;

/// <summary>
/// 浏览器“新标签”检测：合并【浏览器扩展】与【CDP】两个来源。
/// 新标签首次出现后，只要其标题/地址仍在变化（页面加载中），就保持在“新标签”宽限期内，
/// 规则可以命中它最终加载出的标题/地址；内容稳定一段时间后该标签才记为“已见”，不再触发。
/// 这样避免了“标签刚创建时标题为空、被过早消费”导致的漏触发。
/// </summary>
public class BrowserTabService
{
    private readonly CdpService _cdp;
    private readonly TabBridgeServer _bridge;

    // 仍在“新标签”宽限期内的标签：key -> (首次稳定计时, 内容指纹)
    private readonly Dictionary<string, (DateTime FirstSeen, string Fingerprint)> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mature = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(12);

    public BrowserTabService(GeneralSettings general, TabBridgeServer bridge)
    {
        _cdp = new CdpService(general);
        _bridge = bridge;
    }

    /// <summary>返回仍处于“新标签”宽限期内的标签（含后台标签）。</summary>
    public async Task<List<TabInfo>> GetNewTabsAsync()
    {
        var candidates = new List<TabInfo>();
        HashSet<string> covered = new(StringComparer.OrdinalIgnoreCase);

        // 1) 扩展（权威）
        foreach (var t in _bridge.GetSnapshot())
        {
            candidates.Add(t);
            covered.Add(t.Browser);
        }

        // 2) CDP（仅补充未被扩展覆盖的浏览器）
        try
        {
            var cdp = await _cdp.GetTabsAsync();
            foreach (var t in cdp.Tabs)
            {
                if (covered.Contains(t.Browser)) continue;
                candidates.Add(t);
            }
        }
        catch { }

        // 3) 宽限期差量：内容仍在变化或尚未稳定的标签视为“新”
        var result = new List<TabInfo>();
        var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        foreach (var t in candidates)
        {
            var key = string.IsNullOrEmpty(t.Id) ? $"{t.Browser}|{t.Title}|{t.Url}" : $"{t.Browser}|{t.Id}";
            current.Add(key);
            var fp = (t.Title ?? "") + "\u0001" + (t.Url ?? "");

            if (_pending.TryGetValue(key, out var st))
            {
                if (st.Fingerprint != fp)
                    _pending[key] = (now, fp); // 标题/地址还在变，重新计时
                result.Add(New(t));
            }
            else if (!_mature.Contains(key))
            {
                _pending[key] = (now, fp);
                result.Add(New(t));
            }
        }

        // 内容稳定超过宽限期 -> 记为已见
        foreach (var kv in _pending.ToList())
        {
            if (now - kv.Value.FirstSeen >= Grace)
            {
                _pending.Remove(kv.Key);
                _mature.Add(kv.Key);
            }
        }

        // 清理已关闭的标签
        foreach (var k in _pending.Keys.ToList())
            if (!current.Contains(k)) _pending.Remove(k);
        _mature.RemoveWhere(k => !current.Contains(k));

        return result;
    }

    private static TabInfo New(TabInfo t)
        => new() { Title = t.Title, Url = t.Url, Browser = t.Browser, Id = t.Id, IsNew = true };

    public string GetStatus()
    {
        var covered = _bridge.GetCoveredBrowsers();
        int count = _bridge.GetSnapshot().Count;
        string ext = covered.Count > 0
            ? $"扩展已连接：{string.Join(", ", covered)}（{count} 个标签）"
            : "扩展未连接";
        return ext + " ｜ " + _cdp.GetStatusDescription();
    }
}
