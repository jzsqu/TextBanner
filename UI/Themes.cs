using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace TextBanner.UI;

/// <summary>一套“三色调”主题：背景 / 表面 / 强调色，外加派生出的文字、边框、悬停/选中色。</summary>
public class ThemePalette
{
    public string Key { get; set; }
    public string Name { get; set; }
    public bool IsDark { get; set; }
    public string WindowBg { get; set; }
    public string Surface { get; set; }
    public string Hover { get; set; }
    public string Selected { get; set; }
    public string Text { get; set; }
    public string TextSecondary { get; set; }
    public string Border { get; set; }
    public string Accent { get; set; }

    public override string ToString() => Name;
}

public static class ThemeManager
{
    public static readonly ThemePalette[] All =
    {
        new ThemePalette
        {
            Key = "light", Name = "明亮", IsDark = false,
            WindowBg = "#F5F7FA", Surface = "#FFFFFF", Hover = "#EEF1F6", Selected = "#E8F0FE",
            Text = "#1B2430", TextSecondary = "#8A94A6", Border = "#E3E8F0", Accent = "#2F6FED"
        },
        new ThemePalette
        {
            Key = "dark", Name = "夜间", IsDark = true,
            WindowBg = "#16191F", Surface = "#1F242C", Hover = "#262C35", Selected = "#243247",
            Text = "#E6E9EE", TextSecondary = "#8B93A1", Border = "#2C333D", Accent = "#6E9BFF"
        },
        new ThemePalette
        {
            Key = "mint", Name = "薄荷", IsDark = false,
            WindowBg = "#F1F6F4", Surface = "#FFFFFF", Hover = "#EAF1EE", Selected = "#DCEFEA",
            Text = "#1B2A27", TextSecondary = "#7A8C88", Border = "#DFE9E6", Accent = "#2BA471"
        },
        new ThemePalette
        {
            Key = "dusk", Name = "暮紫", IsDark = true,
            WindowBg = "#1A1820", Surface = "#232029", Hover = "#2B2733", Selected = "#2E2538",
            Text = "#E8E6EE", TextSecondary = "#918BA0", Border = "#322E3C", Accent = "#A78BFA"
        },
        new ThemePalette
        {
            Key = "crimson", Name = "绯红", IsDark = true,
            WindowBg = "#1B1416", Surface = "#251A1D", Hover = "#2D2024", Selected = "#3A1F26",
            Text = "#EFE6E8", TextSecondary = "#9B878C", Border = "#372A2E", Accent = "#F2555A"
        },
    };

    private static readonly ThemePalette AutoOption = new() { Key = "auto", Name = "跟随系统" };

    /// <summary>供下拉框使用的选项列表（含“跟随系统”）。</summary>
    public static ThemePalette[] Options => new[] { AutoOption }.Concat(All).ToArray();

    public static string CurrentKey { get; private set; } = "light";

    public static string ResolveKey(string key)
    {
        if (key == "auto") return IsSystemDark() ? "dark" : "light";
        return key;
    }

    public static ThemePalette Get(string key)
    {
        var resolved = ResolveKey(key);
        foreach (var t in All)
            if (t.Key == resolved) return t;
        return All[0];
    }

    public static bool IsSystemDark()
    {
        try
        {
            using var rk = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = rk?.GetValue("AppsUseLightTheme");
            if (v is int i) return i == 0;
        }
        catch { }
        return false;
    }

    /// <summary>把主题写入应用资源（DynamicResource 会自动刷新所有引用它的窗口/控件）。</summary>
    public static void Apply(string key)
    {
        CurrentKey = key;
        var p = Get(key);
        var res = Application.Current.Resources;
        Set(res, "Theme.WindowBg", p.WindowBg);
        Set(res, "Theme.Surface", p.Surface);
        Set(res, "Theme.Hover", p.Hover);
        Set(res, "Theme.Selected", p.Selected);
        Set(res, "Theme.Text", p.Text);
        Set(res, "Theme.TextSecondary", p.TextSecondary);
        Set(res, "Theme.Border", p.Border);
        Set(res, "Theme.Accent", p.Accent);
    }

    /// <summary>当前是“跟随系统”时，按系统最新亮/暗重新应用。</summary>
    public static void ReapplyIfAuto()
    {
        if (CurrentKey == "auto") Apply("auto");
    }

    private static void Set(ResourceDictionary res, string key, string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        res[key] = brush;
    }
}
