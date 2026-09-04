using System.IO;
using System.Text;
using TextBanner.Models;

namespace TextBanner.Services;

public class FileMonitorService
{
    private static readonly string[] Exts =
    {
        "*.txt", "*.log", "*.md", "*.json", "*.csv", "*.ini", "*.yaml", "*.yml", "*.xml", "*.cfg", "*.conf"
    };

    private const int MaxBytes = 4 * 1024 * 1024; // 4MB，够大文本文件用，也防止误读超大二进制

    public List<string> ResolveFiles(string filter)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(filter)) return result;
        filter = filter.Trim().Trim('"');
        try
        {
            if (File.Exists(filter))
            {
                result.Add(Path.GetFullPath(filter));
                return result;
            }

            if (Directory.Exists(filter))
            {
                foreach (var ext in Exts)
                {
                    try { result.AddRange(Directory.GetFiles(filter, ext, SearchOption.TopDirectoryOnly)); }
                    catch { }
                }
                return result;
            }

            if (filter.Contains('*') || filter.Contains('?'))
            {
                var dir = Path.GetDirectoryName(filter);
                if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
                var pattern = Path.GetFileName(filter);
                if (Directory.Exists(dir))
                {
                    try { result.AddRange(Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly)); }
                    catch { }
                }
                return result;
            }
        }
        catch { }
        return result;
    }

    public string ReadText(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > MaxBytes)
                Array.Resize(ref bytes, MaxBytes);
            return Decode(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes.Length == 0) return "";
        // BOM 优先
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        // 先按严格 UTF-8 尝试；失败则回退到系统默认（中文 Windows 通常 GBK）
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            try { return Encoding.GetEncoding(936).GetString(bytes); }
            catch { return Encoding.Default.GetString(bytes); }
        }
    }
}
