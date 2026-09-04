using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using TextBanner.UI;

namespace TextBanner.Services;

public class TrayIconService : IDisposable
{
    private readonly AppServices _app;
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ContextMenuStrip _menu;
    private readonly WinForms.ToolStripMenuItem _openSettingsItem;
    private readonly WinForms.ToolStripMenuItem _rulesRoot;
    private readonly WinForms.ToolStripMenuItem _eventsItem;
    private readonly WinForms.ToolStripMenuItem _pauseItem;
    private readonly WinForms.ToolStripMenuItem _testItem;
    private readonly WinForms.ToolStripMenuItem _exitItem;

    public TrayIconService(AppServices app)
    {
        _app = app;
        _icon = new WinForms.NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "TextBanner",
            Visible = true
        };

        _menu = new WinForms.ContextMenuStrip();
        _openSettingsItem = new WinForms.ToolStripMenuItem();
        _openSettingsItem.Click += (s, e) => _app.OpenSettings();
        _menu.Items.Add(_openSettingsItem);

        _rulesRoot = new WinForms.ToolStripMenuItem();
        _menu.Items.Add(_rulesRoot);

        _eventsItem = new WinForms.ToolStripMenuItem();
        _eventsItem.Click += (s, e) => _app.OpenSettings(eventsTab: true);
        _menu.Items.Add(_eventsItem);

        _menu.Items.Add(new WinForms.ToolStripSeparator());

        _pauseItem = new WinForms.ToolStripMenuItem();
        _pauseItem.Click += (s, e) => _app.TogglePause();
        _menu.Items.Add(_pauseItem);

        _testItem = new WinForms.ToolStripMenuItem();
        _testItem.Click += (s, e) => _app.ShowTest();
        _menu.Items.Add(_testItem);

        _menu.Items.Add(new WinForms.ToolStripSeparator());

        _exitItem = new WinForms.ToolStripMenuItem();
        _exitItem.Click += (s, e) => _app.Exit();
        _menu.Items.Add(_exitItem);

        _menu.Opening += (s, e) => RefreshDynamic();
        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick += (s, e) => _app.OpenSettings();
    }

    private void RefreshDynamic()
    {
        _openSettingsItem.Text = Loc.Get("TrayOpenSettings");
        _rulesRoot.Text = Loc.Get("TrayRules");
        _eventsItem.Text = Loc.Get("TrayEvents");
        _pauseItem.Text = _app.Config.General.Paused ? Loc.Get("TrayResume") : Loc.Get("TrayPause");
        _testItem.Text = Loc.Get("TrayTest");
        _exitItem.Text = Loc.Get("TrayExit");

        _rulesRoot.DropDownItems.Clear();
        var rules = _app.Config.RulesSnapshot();
        if (rules.Count == 0)
        {
            var empty = _rulesRoot.DropDownItems.Add(Loc.Get("NoRules"));
            empty.Enabled = false;
            return;
        }
        foreach (var r in rules)
        {
            var label = (r.Enabled ? "● " : "○ ") + (string.IsNullOrEmpty(r.Name) ? Loc.Get("Unnamed") : r.Name);
            var item = new WinForms.ToolStripMenuItem(label);
            item.Click += (s, e) => _app.OpenSettings(r.Id);
            _rulesRoot.DropDownItems.Add(item);
        }
    }

    private Drawing.Icon CreateIcon()
    {
        var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var bg = new Drawing.SolidBrush(Drawing.Color.FromArgb(47, 111, 237)))
                g.FillEllipse(bg, 1, 1, 30, 30);
            using (var fg = new Drawing.SolidBrush(Drawing.Color.White))
            {
                g.FillEllipse(fg, 8, 6, 6, 6);
                g.FillRectangle(fg, 8, 15, 16, 3);
                g.FillRectangle(fg, 8, 21, 10, 3);
            }
        }
        return Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        try { _icon.Visible = false; _icon.Dispose(); } catch { }
        try { _menu.Dispose(); } catch { }
    }
}
