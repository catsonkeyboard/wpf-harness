using System.Collections.Concurrent;
using WpfHarness.Harness.Abstractions;

namespace WpfHarness.Harness.Tools;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ToolDef> _tools = new(StringComparer.Ordinal);

    public IReadOnlyList<ToolDef> Tools => _tools.Values.ToList();

    public void Register(ToolDef tool) => _tools[tool.Name] = tool;

    public bool Unregister(string name) => _tools.TryRemove(name, out _);

    public ToolDef? Find(string name) => _tools.TryGetValue(name, out var t) ? t : null;

    public async Task<(string Result, bool IsError)> ExecuteAsync(
        string name, string argumentsJson, CancellationToken ct = default)
    {
        if (_tools.TryGetValue(name, out var tool))
        {
            var result = await tool.Handler(argumentsJson, ct);
            return (result, false);
        }
        return ($"工具 {name} 不存在", true);
    }
}
