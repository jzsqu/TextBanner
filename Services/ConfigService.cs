using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextBanner.Models;

namespace TextBanner.Services;

public class ConfigService
{
    private readonly string _path;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppConfig Config { get; private set; }

    public ConfigService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextBanner");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "config.json");
        Config = Load();
        if (!File.Exists(_path))
            Save(); // 首次运行落盘一份默认配置，方便直接查看/修改
    }

    public string ConfigPath => _path;

    public GeneralSettings General => Config.General;

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path), Options);
                if (cfg != null)
                {
                    cfg.General ??= new GeneralSettings();
                    cfg.Rules ??= new List<TriggerRule>();
                    return cfg;
                }
            }
        }
        catch { /* 配置损坏则回退默认 */ }
        return CreateDefault();
    }

    private static AppConfig CreateDefault()
    {
        var cfg = new AppConfig();
        cfg.Rules.Add(new TriggerRule
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "示例：浏览器标签",
            Source = RuleSource.Browser,
            SourceFilter = "",
            MatchText = "示例关键词",
            MatchTarget = "title",
            DisplayText = "检测到新标签页包含「示例关键词」。\n\n**双击本提示**打开设置，可修改或删除这条示例规则。",
            Duration = 8,
            Size = "large",
            Color = "theme"
        });
        return cfg;
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(Config, Options), new UTF8Encoding(true));
                File.Move(tmp, _path, true);
            }
            catch { }
        }
    }

    public List<TriggerRule> RulesSnapshot()
    {
        lock (_lock)
        {
            return new List<TriggerRule>(Config.Rules);
        }
    }

    public void ReplaceRules(IEnumerable<TriggerRule> rules)
    {
        lock (_lock)
        {
            Config.Rules = rules.ToList();
        }
    }
}
