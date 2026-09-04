using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using TextBanner.UI;

namespace TextBanner.Models;

public enum RuleSource
{
    Browser, // 浏览器标签
    Window,  // 窗口名字
    File,    // 文本文件
    Folder   // 文件夹
}

public class TriggerRule : INotifyPropertyChanged
{
    private string _name = "新规则";
    private bool _enabled = true;
    private RuleSource _source = RuleSource.Browser;
    private string _sourceFilter = "";
    private string _matchText = "";
    private string _matchTarget = "title";
    private string _displayText = "";
    private double _duration = 8;
    private string _size = "large";
    private string _color = "#2F6FED";

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public bool Enabled
    {
        get => _enabled;
        set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
    }

    public RuleSource Source
    {
        get => _source;
        set { if (_source != value) { _source = value; OnPropertyChanged(); OnPropertyChanged(nameof(SourceLabel)); OnPropertyChanged(nameof(Summary)); } }
    }

    public string SourceFilter
    {
        get => _sourceFilter;
        set { if (_sourceFilter != value) { _sourceFilter = value; OnPropertyChanged(); } }
    }

    public string MatchText
    {
        get => _matchText;
        set { if (_matchText != value) { _matchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); } }
    }

    public string MatchTarget
    {
        get => _matchTarget;
        set { if (_matchTarget != value) { _matchTarget = value; OnPropertyChanged(); } }
    }

    public string DisplayText
    {
        get => _displayText;
        set { if (_displayText != value) { _displayText = value; OnPropertyChanged(); } }
    }

    public double Duration
    {
        get => _duration;
        set { if (Math.Abs(_duration - value) > 0.001) { _duration = value; OnPropertyChanged(); } }
    }

    public string Size
    {
        get => _size;
        set { if (_size != value) { _size = value; OnPropertyChanged(); } }
    }

    public string Color
    {
        get => _color;
        set { if (_color != value) { _color = value; OnPropertyChanged(); } }
    }

    /// <summary>触发时同时打开的目标：网址 / 文件夹 / 文件路径；留空则不打开。</summary>
    public string ActionTarget { get; set; } = "";

    [JsonIgnore]
    public string SourceLabel => Source switch
    {
        RuleSource.Browser => Loc.Get("SrcLabelBrowser"),
        RuleSource.Window => Loc.Get("SrcLabelWindow"),
        RuleSource.File => Loc.Get("SrcLabelFile"),
        RuleSource.Folder => Loc.Get("SrcLabelFolder"),
        _ => Source.ToString()
    };

    [JsonIgnore]
    public string Summary => $"{SourceLabel} · {Loc.Get("MatchPrefix")}{MatchText}";

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(Summary));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class GeneralSettings
{
    public string Theme { get; set; } = "light"; // light | dark | mint | dusk | crimson | auto
    public string Language { get; set; } = "zh"; // zh | en
    public string BannerPosition { get; set; } = "top-right"; // top-right | bottom-right | top-left | bottom-left
    public int PollIntervalMs { get; set; } = 600;
    public double DefaultDuration { get; set; } = 8;
    public int CooldownSeconds { get; set; } = 10;
    public bool StartWithWindows { get; set; } = false;
    public bool Paused { get; set; } = false;
    public string BrowserProcesses { get; set; } = "chrome;msedge;firefox;brave;opera;vivaldi";
    public string CdpPorts { get; set; } = "9222;9223";
}

public class AppConfig
{
    public List<TriggerRule> Rules { get; set; } = new();
    public GeneralSettings General { get; set; } = new();
}

public class EventRecord : INotifyPropertyChanged
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string RuleName { get; set; } = "";
    public RuleSource? SourceKey { get; set; }
    public string MatchedText { get; set; } = "";

    [JsonIgnore]
    public string Source => SourceKey switch
    {
        RuleSource.Browser => Loc.Get("SrcLabelBrowser"),
        RuleSource.Window => Loc.Get("SrcLabelWindow"),
        RuleSource.File => Loc.Get("SrcLabelFile"),
        RuleSource.Folder => Loc.Get("SrcLabelFolder"),
        _ => Loc.Get("EngineSource"),
    };

    public event PropertyChangedEventHandler PropertyChanged;

    public void RefreshDisplay()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source)));
}

public class WindowInfo
{
    public IntPtr Handle;
    public string Title = "";
    public uint ProcessId;
    public string ProcessName = "";
}

public class TabInfo
{
    public string Title = "";
    public string Url = "";
    public string Browser = "";
    public string Id = "";
    public bool IsNew = false;
}
