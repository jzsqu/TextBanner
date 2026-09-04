using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    private static readonly (string LocKey, string Hex)[] PresetColors =
    {
        ("ColorBlue", "#2F6FED"), ("ColorRed", "#E5484D"), ("ColorGreen", "#30A46C"), ("ColorOrange", "#F76B15"),
        ("ColorPurple", "#8E4EC6"), ("ColorCyan", "#00A2C7"), ("ColorPink", "#E93D82"), ("ColorGray", "#6B7280"),
    };

    public MainWindow(AppServices app)
    {
        _app = app;
        InitializeComponent();

        _rules = new ObservableCollection<TriggerRule>(_app.Config.RulesSnapshot());
        RuleList.ItemsSource = _rules;
        EventList.ItemsSource = _app.EventLog.Records;

        ApplyLanguage();

        if (_rules.Count > 0)
            RuleList.SelectedIndex = 0;
        else
            ShowEmptyEditor();

        UpdateCdpStatus();
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += (_, _) => UpdateCdpStatus();
        _statusTimer.Start();
    }

    // ---------------- 本地化 ----------------

    private void ApplyLanguage()
    {
        Loc.Lang = _app.Config.General.Language;
        SetStaticTexts();
        BuildComboItems();
        LoadGeneralSettings();
        if (_currentRule != null) LoadRule(_currentRule);

        foreach (var r in _rules) r.RefreshDisplay();
        foreach (var rec in _app.EventLog.Records) rec.RefreshDisplay();
        UpdateCdpStatus();
    }

    private void SetStaticTexts()
    {
        Title = Loc.Get("SettingsTitle");
        HeaderTitle.Text = Loc.Get("AppTitle");
        HeaderSubtitle.Text = Loc.Get("AppSubtitle");

        TabRules.Header = Loc.Get("TabRules");
        TabEvents.Header = Loc.Get("TabEvents");
        TabGeneral.Header = Loc.Get("TabGeneral");

        SecBasic.Text = Loc.Get("SecBasic");
        SecTrigger.Text = Loc.Get("SecTrigger");
        SecDisplay.Text = Loc.Get("SecDisplay");
        SecAppearance.Text = Loc.Get("SecAppearance");
        SecBehavior.Text = Loc.Get("SecBehavior");
        SecBrowser.Text = Loc.Get("SecBrowser");
        SecSystem.Text = Loc.Get("SecSystem");

        LblName.Text = Loc.Get("LblName");
        LblSource.Text = Loc.Get("LblSource");
        TargetLabel.Text = Loc.Get("LblMatchTarget");
        LblSourceFilter.Text = Loc.Get("LblSourceFilter");
        LblDisplayText.Text = Loc.Get("LblDisplayText");
        LblAction.Text = Loc.Get("LblAction");
        LblDuration.Text = Loc.Get("LblDuration");
        LblSize.Text = Loc.Get("LblSize");
        LblColor.Text = Loc.Get("LblColor");
        LblTheme.Text = Loc.Get("LblTheme");
        LblLanguage.Text = Loc.Get("LblLanguage");
        LblPosition.Text = Loc.Get("LblPosition");
        LblDefaultDuration.Text = Loc.Get("LblDefaultDuration");
        LblPoll.Text = Loc.Get("LblPoll");
        LblCooldown.Text = Loc.Get("LblCooldown");
        LblBrowserProc.Text = Loc.Get("LblBrowserProc");
        LblCdp.Text = Loc.Get("LblCdp");

        EnabledCheck.Content = Loc.Get("ChkEnabled");
        AutoStartCheck.Content = Loc.Get("ChkAutoStart");
        PausedCheck.Content = Loc.Get("ChkPaused");

        BtnNew.Content = Loc.Get("BtnNew");
        BtnDuplicate.Content = Loc.Get("BtnDuplicate");
        BtnDelete.Content = Loc.Get("BtnDelete");
        BtnTest.Content = Loc.Get("BtnTest");
        BtnClear.Content = Loc.Get("BtnClear");

        HintMatch.Text = Loc.Get("HintMatch");
        HintMarkdown.Text = Loc.Get("HintMarkdown");
        HintAction.Text = Loc.Get("HintAction");
        HintTheme.Text = Loc.Get("HintTheme");
        HintPosition.Text = Loc.Get("HintPosition");
        HintCooldown.Text = Loc.Get("HintCooldown");
        HintBrowserProc.Text = Loc.Get("HintBrowserProc");
        HintCdp.Text = Loc.Get("HintCdp");
        HintEvents.Text = Loc.Get("HintEvents");
        HintAutoSave.Text = Loc.Get("HintAutoSave");
        DurationUnit.Text = Loc.Get("SecUnit");

        BtnBold.ToolTip = Loc.Get("TipBold");
        BtnItalic.ToolTip = Loc.Get("TipItalic");
        BtnStrike.ToolTip = Loc.Get("TipStrike");
        BtnCode.ToolTip = Loc.Get("TipCode");
        BtnLink.ToolTip = Loc.Get("TipLink");
        BtnLink.Content = Loc.Lang == "en" ? "Link" : "链接";
        BtnHeading.ToolTip = Loc.Get("TipHeading");
        BtnList.ToolTip = Loc.Get("TipList");

        ColTime.Header = Loc.Get("ColTime");
        ColRule.Header = Loc.Get("ColRule");
        ColSource.Header = Loc.Get("ColSource");
        ColMatch.Header = Loc.Get("ColMatch");
    }

    private void BuildComboItems()
    {
        SourceCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("SrcBrowser"), RuleSource.Browser),
            new ComboItem(Loc.Get("SrcWindow"), RuleSource.Window),
            new ComboItem(Loc.Get("SrcFile"), RuleSource.File),
            new ComboItem(Loc.Get("SrcFolder"), RuleSource.Folder),
        };
        SourceCombo.DisplayMemberPath = "Label";
        SourceCombo.SelectedValuePath = "Value";

        TargetCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("TargetTitle"), "title"),
            new ComboItem(Loc.Get("TargetUrl"), "url"),
        };
        TargetCombo.DisplayMemberPath = "Label";
        TargetCombo.SelectedValuePath = "Value";

        SizeCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("SizeSmall"), "small"),
            new ComboItem(Loc.Get("SizeMedium"), "medium"),
            new ComboItem(Loc.Get("SizeLarge"), "large"),
        };
        SizeCombo.DisplayMemberPath = "Label";
        SizeCombo.SelectedValuePath = "Value";

        PositionCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("PosTopRight"), "top-right"),
            new ComboItem(Loc.Get("PosBottomRight"), "bottom-right"),
            new ComboItem(Loc.Get("PosTopLeft"), "top-left"),
            new ComboItem(Loc.Get("PosBottomLeft"), "bottom-left"),
        };
        PositionCombo.DisplayMemberPath = "Label";
        PositionCombo.SelectedValuePath = "Value";

        PollCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("Poll300"), 300),
            new ComboItem(Loc.Get("Poll600"), 600),
            new ComboItem(Loc.Get("Poll1000"), 1000),
            new ComboItem(Loc.Get("Poll2000"), 2000),
        };
        PollCombo.DisplayMemberPath = "Label";
        PollCombo.SelectedValuePath = "Value";

        ThemeCombo.ItemsSource = new[]
        {
            new ComboItem(Loc.Get("ThemeAuto"), "auto"),
            new ComboItem(Loc.Get("ThemeLight"), "light"),
            new ComboItem(Loc.Get("ThemeDark"), "dark"),
            new ComboItem(Loc.Get("ThemeMint"), "mint"),
            new ComboItem(Loc.Get("ThemeDusk"), "dusk"),
            new ComboItem(Loc.Get("ThemeCrimson"), "crimson"),
        };
        ThemeCombo.DisplayMemberPath = "Label";
        ThemeCombo.SelectedValuePath = "Value";

        LanguageCombo.ItemsSource = new[]
        {
            new ComboItem("中文", "zh"),
            new ComboItem("English", "en"),
        };
        LanguageCombo.DisplayMemberPath = "Label";
        LanguageCombo.SelectedValuePath = "Value";

        var themeAccent = (Brush)Application.Current.FindResource("Theme.Accent");
        var colorOptions = new List<ColorOption> { new ColorOption(Loc.Get("ColorTheme"), "theme", themeAccent) };
        colorOptions.AddRange(PresetColors.Select(c => new ColorOption(Loc.Get(c.LocKey), c.Hex, Theme.AccentFromHex(c.Hex))));
        ColorCombo.ItemsSource = colorOptions.ToArray();
        ColorCombo.SelectedValuePath = "Hex";
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
            ? (Brush)Application.Current.FindResource("Theme.Accent")
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
                MatchLabel.Text = Loc.Get("MatchLabelBrowser");
                FilterHint.Text = Loc.Get("FilterHintBrowser");
                break;
            case RuleSource.Window:
                MatchLabel.Text = Loc.Get("MatchLabelWindow");
                FilterHint.Text = Loc.Get("FilterHintWindow");
                break;
            case RuleSource.File:
                MatchLabel.Text = Loc.Get("MatchLabelFile");
                FilterHint.Text = Loc.Get("FilterHintFile");
                break;
            case RuleSource.Folder:
                MatchLabel.Text = Loc.Get("MatchLabelFolder");
                FilterHint.Text = Loc.Get("FilterHintFolder");
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
                ? (Brush)Application.Current.FindResource("Theme.Accent")
                : Theme.AccentFromHex(hex);
            MarkDirty();
        }
    }

    private void DurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
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
            Name = Loc.Get("AddRuleName") + " " + (_rules.Count + 1),
            DisplayText = Loc.Get("AddRuleDisplay")
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
        clone.Name = src.Name + Loc.Get("DupSuffix");
        _rules.Add(clone);
        RuleList.SelectedItem = clone;
        Save();
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleList.SelectedItem is not TriggerRule rule) return;
        var res = MessageBox.Show(string.Format(Loc.Get("DelConfirm"), rule.Name), Loc.Get("DelTitle"),
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
                case "bold": WrapSelection("**", "**", Loc.Get("FmtText")); break;
                case "italic": WrapSelection("*", "*", Loc.Get("FmtText")); break;
                case "strike": WrapSelection("~~", "~~", Loc.Get("FmtText")); break;
                case "code": WrapSelection("`", "`", Loc.Get("FmtCode")); break;
                case "link": WrapSelection("[", "](https://)", Loc.Get("FmtLinkText")); break;
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
        LanguageCombo.SelectedValue = g.Language;
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
                ColorPreview.Fill = (Brush)Application.Current.FindResource("Theme.Accent");
        }
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (LanguageCombo.SelectedValue is string lang)
        {
            _app.Config.General.Language = lang;
            _app.Config.Save();
            ApplyLanguage();
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

public sealed record ColorOption(string Name, string Hex, Brush Brush)
{
    public override string ToString() => Name;
}
