using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Storage;

/// <summary>应用配置存储：单个 JSON 文件，ApiKey 落盘前加密。</summary>
public sealed class AppConfigStore
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;

    public AppConfigStore(string? directory = null)
    {
        var dir = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wpf-harness");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "config.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_path)) return new AppConfig();
        try
        {
            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path)) ?? new AppConfig();
            foreach (var p in cfg.Providers)
                p.ApiKey = SecretProtector.Decrypt(p.ApiKeyEncrypted);
            return cfg;
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        foreach (var p in config.Providers)
            p.ApiKeyEncrypted = string.IsNullOrEmpty(p.ApiKey) ? null : SecretProtector.Encrypt(p.ApiKey);
        File.WriteAllText(_path, JsonSerializer.Serialize(config, s_json));
    }
}

/// <summary>会话存储：每个会话一个 JSON 文件，便于导出/同步（数据格式可插拔的基础）。</summary>
public sealed class JsonSessionStore
{
    private static readonly JsonSerializerOptions s_json = new() { WriteIndented = true };

    private readonly string _dir;

    public JsonSessionStore(string? directory = null)
    {
        _dir = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wpf-harness", "sessions");
        Directory.CreateDirectory(_dir);
    }

    private string PathOf(string id) => Path.Combine(_dir, id + ".json");

    public void Save(Session session) =>
        File.WriteAllText(PathOf(session.Id), JsonSerializer.Serialize(session, s_json));

    public List<Session> LoadAll() =>
        Directory.GetFiles(_dir, "*.json")
            .Select(f =>
            {
                try { return JsonSerializer.Deserialize<Session>(File.ReadAllText(f)); }
                catch (JsonException) { return null; }
            })
            .Where(s => s != null)
            .OrderByDescending(s => s!.UpdatedAt)
            .Cast<Session>()
            .ToList();

    public void Delete(string sessionId)
    {
        var p = PathOf(sessionId);
        if (File.Exists(p)) File.Delete(p);
    }
}
