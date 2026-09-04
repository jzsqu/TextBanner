using System.Windows;
using TextBanner.Models;
using TextBanner.UI;

namespace TextBanner.Services;

public class BannerService
{
    private readonly AppServices _app;
    private readonly GeneralSettings _general;
    private readonly List<BannerWindow> _active = new();

    public BannerService(AppServices app, GeneralSettings general)
    {
        _app = app;
        _general = general;
    }

    public void Show(string text, double? duration = null, string size = null, string title = null, string color = null)
    {
        var d = duration ?? _general.DefaultDuration;
        var sz = string.IsNullOrEmpty(size) ? "large" : size;
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var w = new BannerWindow(text, d, sz, title, color);
            w.OpenSettingsRequested += () => _app.OpenSettings();
            w.Loaded += (_, _) => Reposition();
            w.Closed += (_, _) => { _active.Remove(w); Reposition(); };
            _active.Add(w);
            w.Show();
            Reposition();
        });
    }

    private void Reposition()
    {
        try
        {
            var screen = SystemParameters.WorkArea;
            const double gap = 12;
            bool bottom = _general.BannerPosition.StartsWith("bottom");
            bool left = _general.BannerPosition.Contains("left");
            double y = bottom ? screen.Bottom - 8 : screen.Top + 8;

            foreach (var w in _active.ToList())
            {
                if (!w.IsLoaded) continue;
                w.UpdateLayout();
                double x = left ? screen.Left + 8 : screen.Right - w.ActualWidth - 8;
                w.Left = x;
                if (bottom)
                {
                    y -= w.ActualHeight;
                    w.Top = y;
                    y -= gap;
                }
                else
                {
                    w.Top = y;
                    y += w.ActualHeight + gap;
                }
            }
        }
        catch { }
    }
}
