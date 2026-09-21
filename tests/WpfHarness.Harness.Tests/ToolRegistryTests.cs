using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;
using WpfHarness.Harness.Storage;
using WpfHarness.Harness.Tools;

namespace WpfHarness.Harness.Tests;

public class ToolRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_KnownTool_ReturnsHandlerResult()
    {
        var registry = new ToolRegistry();
        registry.Register(new ToolDef
        {
            Name = "echo",
            Handler = (args, _) => Task.FromResult($"got:{args}")
        });

        var (result, isError) = await registry.ExecuteAsync("echo", "{\"a\":1}");

        Assert.False(isError);
        Assert.Equal("got:{\"a\":1}", result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsError()
    {
        var registry = new ToolRegistry();
        var (result, isError) = await registry.ExecuteAsync("nope", "{}");

        Assert.True(isError);
        Assert.Contains("不存在", result);
    }

    [Fact]
    public void Register_SameName_OverwritesRatherThanDuplicates()
    {
        var registry = new ToolRegistry();
        registry.Register(new ToolDef { Name = "t", Description = "v1" });
        registry.Register(new ToolDef { Name = "t", Description = "v2" });

        Assert.Single(registry.Tools);
        Assert.Equal("v2", registry.Find("t")!.Description);
    }

    [Fact]
    public void Unregister_RemovesTool()
    {
        var registry = new ToolRegistry();
        registry.Register(new ToolDef { Name = "t" });

        Assert.True(registry.Unregister("t"));
        Assert.Null(registry.Find("t"));
        Assert.False(registry.Unregister("t"));
    }
}
