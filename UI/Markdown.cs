using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TextBanner.UI;

/// <summary>
/// 轻量 Markdown 渲染：把显示文本解析成 WPF TextBlock Inlines。
/// 支持：**加粗** *斜体* _斜体_ `行内代码` ~~删除线~~ [链接](url) #/##/### 标题 - 列表 --- 分隔线；
/// 并且样式可以**叠加/嵌套**，例如 **加粗*斜体***、**加粗 [链接](url)** 等。
/// </summary>
public static class Markdown
{
    private static readonly Brush CodeBackground = new SolidColorBrush(Color.FromRgb(0xEE, 0xF1, 0xF6));

    public static void Render(TextBlock target, string markdown, Brush accent, double baseFontSize)
    {
        target.Inlines.Clear();
        var text = (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = text.Split('\n');
        bool first = true;

        foreach (var raw in lines)
        {
            if (!first) target.Inlines.Add(new LineBreak());
            first = false;

            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 分隔线
            if (line == "---" || line == "***" || line == "___")
            {
                target.Inlines.Add(new Run("──────") { Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xDC)) });
                continue;
            }

            // 标题
            int level = 0;
            while (level < line.Length && line[level] == '#') level++;
            if (level >= 1 && level <= 3 && (level == line.Length || line[level] == ' '))
            {
                var heading = line.Substring(level).Trim();
                if (heading.Length == 0) continue;
                double size = level == 1 ? baseFontSize + 3 : (level == 2 ? baseFontSize + 1.5 : baseFontSize);
                target.Inlines.Add(new Run(heading)
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = size,
                    Foreground = accent
                });
                continue;
            }

            // 无序列表
            string content = line;
            if ((line.StartsWith("- ") || line.StartsWith("* ")) && line.Length > 2)
            {
                target.Inlines.Add(new Run("•  ") { Foreground = accent, FontWeight = FontWeights.Bold });
                content = line.Substring(2);
            }

            foreach (var inline in ParseInlines(content, 0, content.Length, accent))
                target.Inlines.Add(inline);
        }
    }

    private static List<Inline> ParseInlines(string s, int start, int end, Brush accent)
    {
        var list = new List<Inline>();
        int i = start;
        while (i < end)
        {
            char c = s[i];

            // 转义
            if (c == '\\' && i + 1 < end)
            {
                list.Add(new Run(s[i + 1].ToString()));
                i += 2;
                continue;
            }

            // **加粗** / __加粗__
            if ((c == '*' || c == '_') && i + 1 < end && s[i + 1] == c)
            {
                string delim = s.Substring(i, 2);
                int close = s.IndexOf(delim, i + 2, StringComparison.Ordinal);
                if (close >= i + 2 && close < end)
                {
                    var span = new Span { FontWeight = FontWeights.Bold };
                    AddAll(span.Inlines, ParseInlines(s, i + 2, close, accent));
                    list.Add(span);
                    i = close + 2;
                    continue;
                }
            }

            // *斜体* / _斜体_
            if (c == '*' || c == '_')
            {
                int close = s.IndexOf(c, i + 1);
                if (close > i && close < end)
                {
                    var span = new Span { FontStyle = FontStyles.Italic };
                    AddAll(span.Inlines, ParseInlines(s, i + 1, close, accent));
                    list.Add(span);
                    i = close + 1;
                    continue;
                }
            }

            // ~~删除线~~
            if (c == '~' && i + 1 < end && s[i + 1] == '~')
            {
                int close = s.IndexOf("~~", i + 2, StringComparison.Ordinal);
                if (close > i + 2 && close < end)
                {
                    var span = new Span { TextDecorations = TextDecorations.Strikethrough };
                    AddAll(span.Inlines, ParseInlines(s, i + 2, close, accent));
                    list.Add(span);
                    i = close + 2;
                    continue;
                }
            }

            // `行内代码`
            if (c == '`')
            {
                int close = s.IndexOf('`', i + 1);
                if (close > i && close < end)
                {
                    var run = new Run(s.Substring(i + 1, close - (i + 1)))
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = CodeBackground
                    };
                    list.Add(run);
                    i = close + 1;
                    continue;
                }
            }

            // [链接](url)
            if (c == '[')
            {
                int cb = s.IndexOf(']', i + 1);
                if (cb > i + 1 && cb + 1 < end && s[cb + 1] == '(')
                {
                    int cp = s.IndexOf(')', cb + 2);
                    if (cp > cb + 2 && cp < end)
                    {
                        var linkText = s.Substring(i + 1, cb - (i + 1));
                        var url = s.Substring(cb + 2, cp - (cb + 2));
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            var link = new Hyperlink { Foreground = accent };
                            link.NavigateUri = uri;
                            link.RequestNavigate += (o, e) =>
                            {
                                try { Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true }); }
                                catch { }
                                e.Handled = true;
                            };
                            AddAll(link.Inlines, ParseInlines(linkText, 0, linkText.Length, accent));
                            list.Add(link);
                        }
                        else
                        {
                            AddAll(list, ParseInlines(linkText, 0, linkText.Length, accent));
                        }
                        i = cp + 1;
                        continue;
                    }
                }
            }

            // 普通文本：直到下一个特殊字符
            int next = FindSpecial(s, i + 1, end);
            list.Add(new Run(s.Substring(i, next - i)));
            i = next;
        }
        return list;
    }

    private static void AddAll(InlineCollection target, IEnumerable<Inline> inlines)
    {
        foreach (var inl in inlines) target.Add(inl);
    }

    private static void AddAll(List<Inline> target, IEnumerable<Inline> inlines)
    {
        foreach (var inl in inlines) target.Add(inl);
    }

    private static int FindSpecial(string s, int from, int end)
    {
        for (int i = from; i < end; i++)
        {
            char c = s[i];
            if (c == '*' || c == '_' || c == '`' || c == '[' || c == '~' || c == '\\') return i;
        }
        return end;
    }
}
