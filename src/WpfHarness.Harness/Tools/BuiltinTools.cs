using System.Text.Json;
using WpfHarness.Harness.Abstractions;

namespace WpfHarness.Harness.Tools;

/// <summary>内置演示工具：时间查询 + HTTP GET，作为扩展能力的示例。</summary>
public static class BuiltinTools
{
    public static ToolDef GetCurrentTime() => new()
    {
        Name = "get_current_time",
        Description = "获取本机当前日期时间",
        ParametersJsonSchema = """
            {"type":"object","properties":{"timezone":{"type":"string","description":"IANA 时区，可选，默认本机"}},"required":[]}
            """,
        Handler = (_, _) => Task.FromResult(JsonSerializer.Serialize(new
        {
            now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            timezone = TimeZoneInfo.Local.Id
        }))
    };

    public static ToolDef HttpGet() => new()
    {
        Name = "http_get",
        Description = "发起 HTTP GET 请求并返回响应正文（前 2000 字符），用于获取网页或接口数据",
        ParametersJsonSchema = """
            {"type":"object","properties":{"url":{"type":"string","description":"目标 URL"}},"required":["url"]}
            """,
        Handler = async (argsJson, ct) =>
        {
            var url = JsonSerializer.Deserialize<JsonElement>(argsJson)
                .TryGetProperty("url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("缺少 url 参数");

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("User-Agent", "WpfHarness/0.1");
            var body = await http.GetStringAsync(url, ct);
            return body.Length > 2000 ? body[..2000] + "…(截断)" : body;
        }
    };
}
