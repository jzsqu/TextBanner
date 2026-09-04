using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TextBanner.UI;

public partial class BannerWindow : Window
{
    public event Action OpenSettingsRequested;

    private readonly double _duration;
    private readonly Brush _accent;
    private bool _closing;
    private bool _pinned;
    private bool _pressed;
    private bool _dragging;
    private bool _longPressHandled;
    private Point _pressPoint;
    private DispatcherTimer _pressTimer;
    private DispatcherTimer _autoCloseTimer;

    public BannerWindow(string text, double duration, string size, string title, string color)
    {
        InitializeComponent();
        _duration = duration;
        _accent = (color == "theme" || string.IsNullOrWhiteSpace(color))
            ? (Brush)Application.Current.FindResource("Theme.Accent")
            : Theme.AccentFromHex(color);
        AccentBar.Background = _accent;
        TitleText.Foreground = _accent;
        if (!string.IsNullOrWhiteSpace(title)) TitleText.Text = title;
        ApplySize(size);
        Markdown.Render(MessageText, text, _accent, MessageText.FontSize);
        Opacity = 0;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        StartAutoClose();
    }

    private void ApplySize(string size)
    {
        switch (size)
        {
            case "small":
                Width = 300;
                MessageText.FontSize = 13;
                break;
            case "medium":
                Width = 370;
                MessageText.FontSize = 15;
                break;
            default:
                Width = 450;
                MessageText.FontSize = 16;
                break;
        }
    }

    // ---------------- 交互：双击设置 / 右键关闭 / 长按固定 / 拖动 ----------------

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            OpenSettingsRequested?.Invoke();
            e.Handled = true;
            return;
        }

        _pressed = true;
        _dragging = false;
        _longPressHandled = false;
        _pressPoint = e.GetPosition(this);

        _pressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _pressTimer.Tick += (_, _) =>
        {
            _pressTimer.Stop();
            _longPressHandled = true;
            TogglePin();
        };
        _pressTimer.Start();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed || _dragging || _longPressHandled) return;
        var p = e.GetPosition(this);
        if (Math.Abs(p.X - _pressPoint.X) > 4 || Math.Abs(p.Y - _pressPoint.Y) > 4)
        {
            _dragging = true;
            _pressTimer?.Stop();
            try { DragMove(); } catch { }
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pressed = false;
        _pressTimer?.Stop();
        _dragging = false;
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        CloseBanner();
        e.Handled = true;
    }

    private void TogglePin()
    {
        _pinned = !_pinned;
        if (_pinned)
        {
            _autoCloseTimer?.Stop();
            HintText.Text = "已固定 · 右键或 ✕ 关闭 · 长按取消固定";
            Card.BorderBrush = _accent;
            Card.BorderThickness = new Thickness(2);
        }
        else
        {
            HintText.Text = "双击设置 · 右键关闭 · 长按固定";
            Card.SetResourceReference(Border.BorderBrushProperty, "Theme.Border");
            Card.BorderThickness = new Thickness(1);
            StartAutoClose();
        }
    }

    private void StartAutoClose()
    {
        if (_duration <= 0 || _pinned) return;
        _autoCloseTimer?.Stop();
        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_duration) };
        _autoCloseTimer.Tick += (_, _) => { _autoCloseTimer.Stop(); CloseBanner(); };
        _autoCloseTimer.Start();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseBanner();
    }

    private void CloseBanner()
    {
        if (_closing) return;
        _closing = true;
        _autoCloseTimer?.Stop();
        var a = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        a.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, a);
    }
}
