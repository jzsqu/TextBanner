using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TextBanner.Models;

namespace TextBanner.Services;

/// <summary>
/// 本地桥接服务：监听 127.0.0.1:51739，接收浏览器扩展上报的标签快照。
/// 用 TcpListener 而非 HttpListener，避免 URL ACL / 管理员权限问题。
/// </summary>
public class TabBridgeServer : IDisposable
{
    public const int Port = 51739;

    private readonly object _lock = new();
    private readonly Dictionary<string, List<TabInfo>> _snapshot = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource _cts;
    private TcpListener _listener;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => RunLoop(_cts.Token));
    }

    private async Task RunLoop(CancellationToken ct)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
        }
        catch { return; } // 端口被占用则桥接服务不可用，但不影响其它功能

        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(ct); }
            catch { break; }
            _ = Task.Run(() => HandleClient(client));
        }
        try { _listener.Stop(); } catch { }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                // 读请求头，同时保留可能被一起读入的 body 字节
                var acc = new List<byte>();
                var buf = new byte[8192];
                int headerEnd = -1;
                while (acc.Count < 64 * 1024)
                {
                    int n = await stream.ReadAsync(buf, 0, buf.Length);
                    if (n <= 0) break;
                    acc.AddRange(new ArraySegment<byte>(buf, 0, n));
                    headerEnd = FindHeaderEnd(acc);
                    if (headerEnd >= 0) break;
                }
                if (headerEnd < 0) return;

                string head = Encoding.UTF8.GetString(acc.ToArray(), 0, headerEnd);

                string method = "GET", path = "/";
                int firstLineEnd = head.IndexOf('\n');
                if (firstLineEnd >= 0)
                {
                    var parts = head.Substring(0, firstLineEnd).Trim().Split(' ');
                    if (parts.Length >= 2) { method = parts[0].ToUpperInvariant(); path = parts[1]; }
                }

                int contentLength = 0;
                foreach (var line in head.Split('\n'))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(line.Substring(15).Trim(), out contentLength);
                }

                // 请求头之后已读入的字节就是 body 的开头，继续读完剩余部分
                var body = new List<byte>();
                int bodyStart = headerEnd + 4; // 跳过 \r\n\r\n
                for (int i = bodyStart; i < acc.Count; i++) body.Add(acc[i]);
                while (body.Count < contentLength)
                {
                    var tmp = new byte[Math.Min(8192, contentLength - body.Count)];
                    int n = await stream.ReadAsync(tmp, 0, tmp.Length);
                    if (n <= 0) break;
                    body.AddRange(new ArraySegment<byte>(tmp, 0, n));
                }
                string bodyText = body.Count > 0 ? Encoding.UTF8.GetString(body.ToArray()) : "";

                string responseBody;
                if (method == "GET" && path == "/status")
                {
                    responseBody = BuildStatusJson();
                }
                else
                {
                    ParseAndStore(bodyText);
                    responseBody = "{}";
                }

                await WriteResponse(stream, responseBody);
            }
        }
        catch { }
    }

    private string BuildStatusJson()
    {
        lock (_lock)
        {
            var browsers = new Dictionary<string, int>();
            int total = 0;
            foreach (var kv in _snapshot)
            {
                browsers[kv.Key] = kv.Value.Count;
                total += kv.Value.Count;
            }
            return JsonSerializer.Serialize(new { browsers, total });
        }
    }

    private static async Task WriteResponse(NetworkStream stream, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var header = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, 0, header.Length);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static int FindHeaderEnd(List<byte> b)
    {
        for (int i = 0; i + 3 < b.Count; i++)
        {
            if (b[i] == (byte)'\r' && b[i + 1] == (byte)'\n' && b[i + 2] == (byte)'\r' && b[i + 3] == (byte)'\n')
                return i;
        }
        return -1;
    }

    private void ParseAndStore(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string browser = root.TryGetProperty("browser", out var b) ? (b.GetString() ?? "chrome") : "chrome";
            var list = new List<TabInfo>();
            if (root.TryGetProperty("tabs", out var tabs) && tabs.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in tabs.EnumerateArray())
                {
                    string id = "";
                    if (t.TryGetProperty("id", out var idEl))
                        id = idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt32().ToString() : (idEl.GetString() ?? "");
                    string title = t.TryGetProperty("title", out var ti) ? (ti.GetString() ?? "") : "";
                    string url = t.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "";
                    list.Add(new TabInfo { Id = id, Title = title, Url = url, Browser = browser });
                }
            }
            lock (_lock) { _snapshot[browser] = list; }
        }
        catch { }
    }

    public List<TabInfo> GetSnapshot()
    {
        lock (_lock)
        {
            var r = new List<TabInfo>();
            foreach (var kv in _snapshot) r.AddRange(kv.Value);
            return r;
        }
    }

    public HashSet<string> GetCoveredBrowsers()
    {
        lock (_lock) { return new HashSet<string>(_snapshot.Keys, StringComparer.OrdinalIgnoreCase); }
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
    }
}
