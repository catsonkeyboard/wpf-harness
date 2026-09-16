using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Modes;
using WpfHarness.Harness.Tools;

namespace WpfHarness.Harness;

/// <summary>
/// 组合根（pi 风格）：注册模式 / Provider 工厂 / 工具 / 扩展，
/// 构建 HarnessEngine。扩展通过 Configure 钩子向 Builder 注入能力。
/// </summary>
public sealed class HarnessBuilder
{
    public IToolRegistry Tools { get; } = new ToolRegistry();

    public Dictionary<string, IAgentMode> Modes { get; } = new();

    public Dictionary<string, IChatProviderFactory> ProviderFactories { get; } = new();

    public List<IAgentEventSubscriber> EventSubscribers { get; } = new();

    public HarnessBuilder AddMode(IAgentMode mode)
    {
        Modes[mode.Id] = mode;
        return this;
    }

    public HarnessBuilder AddProviderFactory(IChatProviderFactory factory)
    {
        ProviderFactories[factory.Type] = factory;
        return this;
    }

    public HarnessBuilder AddTool(ToolDef tool)
    {
        Tools.Register(tool);
        return this;
    }

    public HarnessBuilder AddEventSubscriber(IAgentEventSubscriber subscriber)
    {
        EventSubscribers.Add(subscriber);
        return this;
    }

    public HarnessBuilder UseExtension(IHarnessExtension extension)
    {
        extension.Configure(this);
        return this;
    }

    public HarnessEngine Build()
    {
        if (Modes.Count == 0)
        {
            AddMode(new ChatMode());
            AddMode(new ReActMode());
            AddMode(new ReActTextMode());
        }
        if (ProviderFactories.Count == 0)
        {
            AddProviderFactory(new OpenAiCompatProviderFactory());
            AddProviderFactory(new MockProviderFactory());
        }
        return new HarnessEngine(Modes, ProviderFactories, Tools, EventSubscribers.ToList());
    }
}
