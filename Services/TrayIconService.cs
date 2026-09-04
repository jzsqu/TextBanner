using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace TextBanner.Services;

public class TrayIconService : IDisposable
{
    private readonly AppServices _app;
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ContextMenuStrip _menu;
    private readonly WinForms.ToolStripMenuItem _pauseItem;
    private readonly WinForms.ToolStripMenuItem _rulesRoot;

    public TrayIconService(AppServices app)
    {
        _app = app;
        _icon = new WinForms.NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "文本触发提醒 TextBanner",
            Visible = true
        };

        _menu = new WinForms.ContextMenuStrip();
        _menu.Items.Add("打开设置", null, (s, e) => _app.OpenSettings());
        _rulesRoot = new WinForms.ToolStripMenuItem("规则列表");
        _menu.Items.Add(_rulesRoot);
        _menu.Items.Add("事件记录", null, (s, e) => _app.OpenSettings(eventsTab: true));
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _pauseItem = new WinForms.ToolStripMenuItem("暂停监控");
        _pauseItem.Click += (s, e) => _app.TogglePause();
        _menu.Items.Add(_pauseItem);
        _menu.Items.Add("测试提示", null, (s, e) => _app.ShowTest());
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("退出", null, (s, e) => _app.Exit());

        _menu.Opening += (s, e) => RefreshDynamic();
        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick += (s, e) => _app.OpenSettings();
    }

    private void RefreshDynamic()
    {
        _pauseItem.Text = _app.Config.General.Paused ? "恢复监控" : "暂停监控";

        _rulesRoot.DropDownItems.Clear();
        var rules = _app.Config.RulesSnapshot();
        if (rules.Count == 0)
        {
            var empty = _rulesRoot.DropDownItems.Add("（无规则）");
            empty.Enabled = false;
            return;
        }
        foreach (var r in rules)
        {
            var label = (r.Enabled ? "● " : "○ ") + (string.IsNullOrEmpty(r.Name) ? "（未命名）" : r.Name);
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
