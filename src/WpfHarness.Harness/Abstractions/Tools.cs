namespace WpfHarness.Harness.Abstractions;

/// <summary>
/// 工具定义：JSON Schema 描述参数 + 执行委托。
/// 执行委托收发明文 JSON 字符串，返回字符串结果，异常视为 IsError。
/// </summary>
public sealed class ToolDef
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>OpenAI 风格的参数 JSON Schema，例如 {"type":"object","properties":{...}}。</summary>
    public string ParametersJsonSchema { get; init; } = "{\"type\":\"object\",\"properties\":{}}";

    public Func<string, CancellationToken, Task<string>> Handler { get; init; } =
        (_, _) => Task.FromResult("{}");
}

public interface IToolRegistry
{
    IReadOnlyList<ToolDef> Tools { get; }

    void Register(ToolDef tool);

    bool Unregister(string name);

    ToolDef? Find(string name);

    Task<(string Result, bool IsError)> ExecuteAsync(string name, string argumentsJson, CancellationToken ct = default);
}
