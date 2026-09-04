using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TextBanner.Models;
using TextBanner.Services;

namespace TextBanner.UI;

public partial class MainWindow : Window
{
    private readonly AppServices _app;
    private readonly ObservableCollection<TriggerRule> _rules;
    private TriggerRule _currentRule;
    private bool _loading;
    private DispatcherTimer _saveTimer;
    private DispatcherTimer _statusTimer;

    private static readonly (string Name, string Hex)[] PresetColors =
    {
        ("蓝色", "#2F6FED"), ("红色", "#E5484D"), ("绿色", "#30A46C"), ("橙色", "#F76B15"),
        ("紫色", "#8E4EC6"), ("青色", "#00A2C7"), ("粉色", "#E93D82"), ("灰色", "#6B7280"),
    };

    public MainWindow(AppServices app)
    {
        _app = app;
        InitializeComponent();

        _rules = new ObservableCollection<TriggerRule>(_app.Config.RulesSnapshot());
        RuleList.ItemsSource = _rules;
        EventList.ItemsSource = _app.EventLog.Records;

        SourceCombo.ItemsSource = new[]
        {
            new ComboItem("浏览器标签", RuleSource.Browser),
            new ComboItem("窗口名字", RuleSource.Window),
            new ComboItem("文本文件", RuleSource.File),
            new ComboItem("文件夹", RuleSource.Folder),
        };
        SourceCombo.DisplayMemberPath = "Label";
        SourceCombo.SelectedValuePath = "Value";

        TargetCombo.ItemsSource = new[]
        {
            new ComboItem("标签标题", "title"),
            new ComboItem("标签地址 URL", "url"),
        };
        TargetCombo.DisplayMemberPath = "Label";
        TargetCombo.SelectedValuePath = "Value";

        SizeCombo.ItemsSource = new[]
        {
            new ComboItem("小", "small"),
            new ComboItem("中", "medium"),
            new ComboItem("大", "large"),
        };
        SizeCombo.DisplayMemberPath = "Label";
        SizeCombo.SelectedValuePath = "Value";

        var themeAccent = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Theme.Accent");
        var colorOptions = new List<ColorOption> { new ColorOption("跟随主题", "theme", themeAccent) };
        colorOptions.AddRange(PresetColors.Select(c => new ColorOption(c.Name, c.Hex, Theme.AccentFromHex(c.Hex))));
        ColorCombo.ItemsSource = colorOptions.ToArray();
        ColorCombo.SelectedValuePath = "Hex";

        PositionCombo.ItemsSource = new[]
        {
            new ComboItem("右上角", "top-right"),
            new ComboItem("右下角", "bottom-right"),
            new ComboItem("左上角", "top-left"),
            new ComboItem("左下角", "bottom-left"),
        };
        PositionCombo.DisplayMemberPath = "Label";
        PositionCombo.SelectedValuePath = "Value";

        PollCombo.ItemsSource = new[]
        {
            new ComboItem("300 毫秒（灵敏）", 300),
            new ComboItem("600 毫秒（默认）", 600),
            new ComboItem("1000 毫秒", 1000),
            new ComboItem("2000 毫秒（省电）", 2000),
        };
        PollCombo.DisplayMemberPath = "Label";
        PollCombo.SelectedValuePath = "Value";

        ThemeCombo.ItemsSource = ThemeManager.Options;
        ThemeCombo.DisplayMemberPath = "Name";
        ThemeCombo.SelectedValuePath = "Key";

        LoadGeneralSettings();

        if (_rules.Count > 0)
            RuleList.SelectedIndex = 0;
        else
            ShowEmptyEditor();

        UpdateCdpStatus();
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += (_, _) => UpdateCdpStatus();
        _statusTimer.Start();
    }

    // ---------------- 规则编辑 ----------------

    private void RuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RuleList.SelectedItem is TriggerRule rule)
            LoadRule(rule);
        else
            ShowEmptyEditor();
    }

    private void LoadRule(TriggerRule rule)
    {
        _loading = true;
        _currentRule = rule;
        EditorPanel.Visibility = Visibility.Visible;

        NameBox.Text = rule.Name;
        EnabledCheck.IsChecked = rule.Enabled;
        SourceCombo.SelectedValue = rule.Source;
        MatchBox.Text = rule.MatchText;
        TargetCombo.SelectedValue = rule.MatchTarget;
        FilterBox.Text = rule.SourceFilter;
        DisplayBox.Text = rule.DisplayText;
        ActionBox.Text = rule.ActionTarget;
        DurationSlider.Value = rule.Duration;
        DurationBox.Text = rule.Duration.ToString("0.#");
        SizeCombo.SelectedValue = rule.Size;
        ColorCombo.SelectedValue = rule.Color;
        ColorPreview.Fill = rule.Color == "theme"
            ? (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Theme.Accent")
            : Theme.AccentFromHex(rule.Color);
        UpdateSourceUi(rule.Source);

        _loading = false;
    }

    private void ShowEmptyEditor()
    {
        _loading = true;
        _currentRule = null;
        EditorPanel.Visibility = Visibility.Collapsed;
        _loading = false;
    }

    private void UpdateSourceUi(RuleSource source)
    {
        bool isBrowser = source == RuleSource.Browser;
        TargetLabel.Visibility = isBrowser ? Visibility.Visible : Visibility.Collapsed;
        TargetCombo.Visibility = isBrowser ? Visibility.Visible : Visibility.Collapsed;

        switch (source)
        {
            case RuleSource.Browser:
                MatchLabel.Text = "匹配文本（新标签页的标题 / 地址中包含）";
                FilterHint.Text = "来源限定：仅匹配这些浏览器进程名（留空 = 所有浏览器），例如 chrome;msedge。仅在『新打开标签页』时触发一次";
                break;
            case RuleSource.Window:
                MatchLabel.Text = "匹配文本（窗口标题中包含）";
                FilterHint.Text = "来源限定：窗口标题还需包含这些文字（留空 = 任意窗口），例如 记事本";
                break;
            case RuleSource.File:
                MatchLabel.Text = "匹配文本（文件内容中包含）";
                FilterHint.Text = "来源限定：要监视的文件 / 文件夹 / 通配符，例如 D:\\logs\\*.txt（文件夹会监视其中常见文本文件）";
                break;
            case RuleSource.Folder:
                MatchLabel.Text = "匹配文本（打开的文件夹路径中包含）";
                FilterHint.Text = "例如 D:\\某个文件夹 或 文件夹名；在该文件夹于资源管理器中被打开时触发一次（来源限定可留空）";
                break;
        }
    }

    private void Text_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        if (ReferenceEquals(sender, NameBox)) _currentRule.Name = NameBox.Text;
        else if (ReferenceEquals(sender, MatchBox)) _currentRule.MatchText = MatchBox.Text;
        else if (ReferenceEquals(sender, FilterBox)) _currentRule.SourceFilter = FilterBox.Text;
        else if (ReferenceEquals(sender, DisplayBox)) _currentRule.DisplayText = DisplayBox.Text;
        else if (ReferenceEquals(sender, ActionBox)) _currentRule.ActionTarget = ActionBox.Text;
        MarkDirty();
    }

    private void EnabledCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        _currentRule.Enabled = EnabledCheck.IsChecked == true;
        MarkDirty();
    }

    private void SourceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        if (SourceCombo.SelectedValue is RuleSource src)
        {
            _currentRule.Source = src;
            UpdateSourceUi(src);
            MarkDirty();
        }
    }

    private void TargetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        if (TargetCombo.SelectedValue is string t)
        {
            _currentRule.MatchTarget = t;
            MarkDirty();
        }
    }

    private void SizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        if (SizeCombo.SelectedValue is string s)
        {
            _currentRule.Size = s;
            MarkDirty();
        }
    }

    private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _currentRule == null) return;
        if (ColorCombo.SelectedValue is string hex)
        {
            _currentRule.Color = hex;
            ColorPreview.Fill = hex == "theme"
                ? (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Theme.Accent")
                : Theme.AccentFromHex(hex);
            MarkDirty();
        }
    }

    private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // XAML 加载时 Slider 设置 Minimum/Maximum 会提前触发 ValueChanged，此时 DurationBox 尚未构造
        if (_loading || DurationBox == null || _currentRule == null) return;
        DurationBox.Text = e.NewValue.ToString("0.#");
        _currentRule.Duration = e.NewValue;
        MarkDirty();
    }

    private void DurationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_currentRule == null) return;
        if (double.TryParse(DurationBox.Text, out double v))
        {
            v = Math.Clamp(v, 1, 120);
            _currentRule.Duration = v;
        }
        DurationBox.Text = _currentRule.Duration.ToString("0.#");
        DurationSlider.Value = _currentRule.Duration;
        Save();
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        var rule = new TriggerRule
        {
            Name = "新规则 " + (_rules.Count + 1),
            DisplayText = "这是新的触发提示内容"
        };
        _rules.Add(rule);
        RuleList.SelectedItem = rule;
        Save();
    }

    private void DuplicateRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is not TriggerRule src) return;
        var clone = CloneRule(src);
        clone.Id = Guid.NewGuid().ToString("N");
        clone.Name = src.Name + "（副本）";
        _rules.Add(clone);
        RuleList.SelectedItem = clone;
        Save();
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is not TriggerRule rule) return;
        var res = System.Windows.MessageBox.Show($"确定删除规则“{rule.Name}”吗？", "删除规则",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;
        _rules.Remove(rule);
        Save();
    }

    private static TriggerRule CloneRule(TriggerRule src) => new()
    {
        Id = src.Id,
        Name = src.Name,
        Enabled = src.Enabled,
        Source = src.Source,
        SourceFilter = src.SourceFilter,
        MatchText = src.MatchText,
        MatchTarget = src.MatchTarget,
        DisplayText = src.DisplayText,
        Duration = src.Duration,
        Size = src.Size,
        Color = src.Color,
        ActionTarget = src.ActionTarget,
    };

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRule != null)
            _app.Banner.Show(_currentRule.DisplayText, _currentRule.Duration, _currentRule.Size, _currentRule.Name, _currentRule.Color);
    }

    private void FormatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRule == null) return;
        if (sender is Button b && b.Tag is string tag)
        {
            switch (tag)
            {
                case "bold": WrapSelection("**", "**", "文字"); break;
                case "italic": WrapSelection("*", "*", "文字"); break;
                case "strike": WrapSelection("~~", "~~", "文字"); break;
                case "code": WrapSelection("`", "`", "代码"); break;
                case "link": WrapSelection("[", "](https://)", "链接文字"); break;
                case "heading": PrefixLine("# "); break;
                case "list": PrefixLine("- "); break;
            }
        }
    }

    private void WrapSelection(string prefix, string suffix, string placeholder)
    {
        int start = DisplayBox.SelectionStart;
        int len = DisplayBox.SelectionLength;
        string sel = len > 0 ? DisplayBox.SelectedText : placeholder;
        DisplayBox.SelectedText = prefix + sel + suffix;
        DisplayBox.SelectionStart = start + prefix.Length;
        DisplayBox.SelectionLength = sel.Length;
        DisplayBox.Focus();
    }

    private void PrefixLine(string prefix)
    {
        int pos = DisplayBox.SelectionStart;
        int lineStart = DisplayBox.Text.LastIndexOf('\n', Math.Max(0, pos - 1)) + 1;
        DisplayBox.Select(lineStart, 0);
        DisplayBox.SelectedText = prefix;
        DisplayBox.SelectionStart = pos + prefix.Length;
        DisplayBox.Focus();
    }

    private void ClearEvents_Click(object sender, RoutedEventArgs e)
    {
        _app.EventLog.Clear();
    }

    // ---------------- 常规设置 ----------------

    private void LoadGeneralSettings()
    {
        _loading = true;
        var g = _app.Config.General;
        ThemeCombo.SelectedValue = g.Theme;
        PositionCombo.SelectedValue = g.BannerPosition;
        DefaultDurationBox.Text = g.DefaultDuration.ToString("0.#");
        PollCombo.SelectedValue = g.PollIntervalMs;
        CooldownBox.Text = g.CooldownSeconds.ToString();
        BrowserProcessesBox.Text = g.BrowserProcesses;
        CdpPortsBox.Text = g.CdpPorts;
        AutoStartCheck.IsChecked = g.StartWithWindows;
        PausedCheck.IsChecked = g.Paused;
        _loading = false;
    }

    private void PositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (PositionCombo.SelectedValue is string pos)
        {
            _app.Config.General.BannerPosition = pos;
            _app.Config.Save();
        }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (ThemeCombo.SelectedValue is string key)
        {
            _app.Config.General.Theme = key;
            ThemeManager.Apply(key);
            _app.Config.Save();
            if (_currentRule != null && _currentRule.Color == "theme")
                ColorPreview.Fill = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("Theme.Accent");
        }
    }

    private void PollCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (PollCombo.SelectedValue is int ms)
        {
            _app.Config.General.PollIntervalMs = ms;
            _app.Config.Save();
        }
    }

    private void DefaultDurationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(DefaultDurationBox.Text, out double v))
        {
            _app.Config.General.DefaultDuration = Math.Clamp(v, 1, 600);
        }
        DefaultDurationBox.Text = _app.Config.General.DefaultDuration.ToString("0.#");
        _app.Config.Save();
    }

    private void CooldownBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(CooldownBox.Text, out int v))
        {
            _app.Config.General.CooldownSeconds = Math.Clamp(v, 0, 3600);
        }
        CooldownBox.Text = _app.Config.General.CooldownSeconds.ToString();
        _app.Config.Save();
    }

    private void BrowserProcessesBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _app.Config.General.BrowserProcesses = BrowserProcessesBox.Text.Trim();
        _app.Config.Save();
    }

    private void CdpPortsBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _app.Config.General.CdpPorts = CdpPortsBox.Text.Trim();
        _app.Config.Save();
    }

    private void AutoStartCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _app.Config.General.StartWithWindows = AutoStartCheck.IsChecked == true;
        _app.SetAutoStart(_app.Config.General.StartWithWindows);
        _app.Config.Save();
    }

    private void PausedCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _app.Config.General.Paused = PausedCheck.IsChecked == true;
        _app.Config.Save();
    }

    // ---------------- 保存 ----------------

    private void MarkDirty()
    {
        if (_loading) return;
        if (_saveTimer == null)
        {
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Save(); };
        }
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Save()
    {
        _app.Config.ReplaceRules(_rules);
        _app.Config.Save();
    }

    public void SelectRule(string ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule != null)
        {
            RuleList.SelectedItem = rule;
            RuleList.ScrollIntoView(rule);
        }
    }

    public void ShowEventsTab()
    {
        MainTabs.SelectedIndex = 1;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        _statusTimer?.Stop();
        Save();
    }

    private void UpdateCdpStatus()
    {
        CdpStatusText.Text = _app.BrowserTabs.GetStatus();
    }
}

public sealed record ColorOption(string Name, string Hex, System.Windows.Media.Brush Brush);
