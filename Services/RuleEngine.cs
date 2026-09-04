using System.IO;
using TextBanner.Models;
using TextBanner.UI;

namespace TextBanner.Services;

public class RuleEngine
{
    private readonly AppServices _app;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, RuleState> _states = new();
    private Task _task;

    public RuleEngine(AppServices app)
    {
        _app = app;
    }

    public void Start()
    {
        _task = Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        try { _cts.Cancel(); } catch { }
    }

    private async Task RunLoop(CancellationToken ct)
    {
        bool firstIteration = true;
        bool baseline = true; // 首次非暂停扫描只建立基线、不触发

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(firstIteration ? 0 : _app.Config.General.PollIntervalMs, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            firstIteration = false;

            try
            {
                if (_app.Config.General.Paused) continue;

                var now = DateTime.UtcNow;
                var rules = _app.Config.RulesSnapshot();
                bool needWindows = rules.Any(r => r.Enabled && r.Source == RuleSource.Window);
                bool needTabs = rules.Any(r => r.Enabled && r.Source == RuleSource.Browser);
                bool needFolders = rules.Any(r => r.Enabled && r.Source == RuleSource.Folder);
                WindowInfo[] windows = needWindows ? _app.WindowMonitor.GetWindows() : null;
                List<TabInfo> tabs = needTabs ? await _app.BrowserTabs.GetNewTabsAsync() : null;
                List<string> folders = needFolders ? _app.Folders.GetOpenFolders() : null;

                foreach (var rule in rules)
                {
                    if (!rule.Enabled)
                    {
                        EnsureState(rule).Matched = false;
                        continue;
                    }

                    string snippet = "";
                    bool matched;
                    switch (rule.Source)
                    {
                        case RuleSource.Window:
                            matched = MatchWindow(rule, windows, out snippet);
                            break;
                        case RuleSource.Browser:
                            matched = MatchBrowser(rule, tabs, out snippet);
                            break;
                        case RuleSource.File:
                            matched = MatchFile(rule, now, out snippet);
                            break;
                        case RuleSource.Folder:
                            matched = MatchFolder(rule, folders, out snippet);
                            break;
                        default:
                            matched = false;
                            break;
                    }

                    var st = EnsureState(rule);
                    bool cooldownOk = (now - st.LastFired).TotalSeconds >= _app.Config.General.CooldownSeconds;
                    if (!baseline && matched && !st.Matched && cooldownOk)
                    {
                        st.LastFired = now;
                        Fire(rule, snippet);
                    }
                    st.Matched = matched;
                }

                baseline = false;
            }
            catch (Exception ex)
            {
                try
                {
                    _app.EventLog.Record(new EventRecord
                    {
                        Time = DateTime.Now,
                        RuleName = Loc.Get("EngineError"),
                        SourceKey = null,
                        MatchedText = ex.Message
                    });
                }
                catch { }
            }
        }
    }

    private bool MatchWindow(TriggerRule rule, WindowInfo[] windows, out string snippet)
    {
        snippet = "";
        if (windows == null) return false;
        foreach (var w in windows)
        {
            if (!TextMatcher.ContainsFilter(w.Title, rule.SourceFilter)) continue;
            var m = TextMatcher.MatchAny(w.Title, rule.MatchText);
            if (m != null)
            {
                snippet = w.Title;
                return true;
            }
        }
        snippet = "";
        return false;
    }

    private bool MatchBrowser(TriggerRule rule, List<TabInfo> tabs, out string snippet)
    {
        snippet = "";
        if (tabs == null) return false;
        foreach (var t in tabs)
        {
            if (!TextMatcher.ContainsFilter(t.Browser, rule.SourceFilter)) continue;
            var hay = rule.MatchTarget == "url" ? t.Url : t.Title;
            if (string.IsNullOrEmpty(hay)) continue;
            var m = TextMatcher.MatchAny(hay, rule.MatchText);
            if (m != null)
            {
                snippet = hay;
                return true;
            }
        }
        snippet = "";
        return false;
    }

    private bool MatchFile(TriggerRule rule, DateTime now, out string snippet)
    {
        var st = EnsureState(rule);
        // 文件内容检查相对较重，限制最短间隔，避免大文件被高频反复读取
        if ((now - st.LastFileCheck).TotalMilliseconds < 1200)
        {
            snippet = st.LastSnippet;
            return st.Matched;
        }
        st.LastFileCheck = now;

        var files = _app.Files.ResolveFiles(rule.SourceFilter);
        foreach (var f in files)
        {
            var content = _app.Files.ReadText(f);
            if (content == null) continue;
            var m = TextMatcher.MatchAny(content, rule.MatchText);
            if (m != null)
            {
                snippet = st.LastSnippet = $"文件 {Path.GetFileName(f)} 包含“{m}”";
                return true;
            }
        }
        snippet = st.LastSnippet = "";
        return false;
    }

    private bool MatchFolder(TriggerRule rule, List<string> folders, out string snippet)
    {
        snippet = "";
        if (folders == null) return false;
        foreach (var f in folders)
        {
            if (!TextMatcher.ContainsFilter(f, rule.SourceFilter)) continue;
            var m = TextMatcher.MatchAny(f, rule.MatchText);
            if (m != null)
            {
                snippet = f;
                return true;
            }
        }
        snippet = "";
        return false;
    }

    private void Fire(TriggerRule rule, string snippet)
    {
        _app.Banner.Show(rule.DisplayText, rule.Duration, rule.Size, rule.Name, rule.Color);
        _app.EventLog.Record(new EventRecord
        {
            Time = DateTime.Now,
            RuleName = rule.Name,
            SourceKey = rule.Source,
            MatchedText = snippet
        });
        OpenAction(rule.ActionTarget);
    }

    private static void OpenAction(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return;
        foreach (var t in target.Split(new[] { ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(t) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }
    }

    private RuleState EnsureState(TriggerRule rule)
    {
        if (!_states.TryGetValue(rule.Id, out var st))
        {
            st = new RuleState();
            _states[rule.Id] = st;
        }
        return st;
    }

    private class RuleState
    {
        public bool Matched;
        public DateTime LastFired = DateTime.MinValue;
        public DateTime LastFileCheck = DateTime.MinValue;
        public string LastSnippet = "";
    }
}
