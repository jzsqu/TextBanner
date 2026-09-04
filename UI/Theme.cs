using System.Windows.Media;

namespace TextBanner.UI;

public static class Theme
{
    public static SolidColorBrush AccentFromHex(string hex, string fallback = "#2F6FED")
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(c);
        }
        catch
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback)); }
            catch { return new SolidColorBrush(Colors.SteelBlue); }
        }
    }
}
