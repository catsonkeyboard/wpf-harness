using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Tools;

namespace WpfHarness.Harness.Extensions;

/// <summary>
/// 内置工具扩展：示范扩展如何向 Harness 注入工具。
/// 后续扩展（MCP、文件系统、检索等）按同一模式实现即可。
/// </summary>
public sealed class BuiltinToolsExtension : IHarnessExtension
{
    public string Id => "builtin-tools";

    public void Configure(HarnessBuilder builder)
    {
        builder.AddTool(BuiltinTools.GetCurrentTime());
        builder.AddTool(BuiltinTools.HttpGet());
    }
}
