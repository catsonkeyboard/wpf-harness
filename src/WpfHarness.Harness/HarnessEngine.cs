using System.Threading.Channels;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness;

/// <summary>
/// 运行引擎：解析模式与 Provider，驱动模式循环，
/// 把模式内部 Emit 的事件桥接为异步事件流，并广播给订阅者。
/// </summary>
public sealed class HarnessEngine
{
    private readonly Dictionary<string, IAgentMode> _modes;
    private readonly Dictionary<string, IChatProviderFactory> _providerFactories;
    private readonly IToolRegistry _tools;
    private readonly List<IAgentEventSubscriber> _subscribers;

    internal HarnessEngine(
        Dictionary<string, IAgentMode> modes,
        Dictionary<string, IChatProviderFactory> providerFactories,
        IToolRegistry tools,
        List<IAgentEventSubscriber> subscribers)
    {
        _modes = modes;
        _providerFactories = providerFactories;
        _tools = tools;
        _subscribers = subscribers;
    }

    public IReadOnlyList<IAgentMode> AvailableModes => _modes.Values.ToList();

    public IToolRegistry Tools => _tools;

    public IChatProvider CreateProvider(ProviderConfig config)
    {
        var factory = _providerFactories.TryGetValue(config.Type, out var f)
            ? f
            : throw new InvalidOperationException($"未注册的 Provider 类型: {config.Type}");
        return factory.Create(config);
    }

    public IAgentMode ResolveMode(string modeId) =>
        _modes.TryGetValue(modeId, out var mode)
            ? mode
            : throw new InvalidOperationException($"未注册的模式: {modeId}");

    /// <summary>运行一次 Agent：返回增量事件流（Channel 桥接），调用方消费即驱动循环。</summary>
    public IAsyncEnumerable<AgentEvent> RunAsync(
        Session session,
        ProviderConfig providerConfig,
        string userInput,
        AgentOptions? options = null,
        CancellationToken ct = default)
    {
        var mode = ResolveMode(session.ModeId);
        var provider = CreateProvider(providerConfig);
        options ??= new AgentOptions();

        var channel = Channel.CreateUnbounded<AgentEvent>(
            new UnboundedChannelOptions { SingleReader = true });

        var ctx = new AgentRunContext
        {
            Session = session,
            Provider = provider,
            Model = session.Model,
            Tools = _tools,
            Options = options,
            Emit = ev =>
            {
                channel.Writer.TryWrite(ev);
                foreach (var sub in _subscribers)
                    sub.OnEvent(session.Id, ev);
            }
        };

        var pump = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in mode.RunAsync(ctx, userInput, ct)) { }
                channel.Writer.TryComplete();
            }
            catch (OperationCanceledException)
            {
                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryWrite(new AgentFailed(ex));
                channel.Writer.TryComplete();
            }
        }, ct);

        return ReadChannelAsync(channel, ct);
    }

    private static async IAsyncEnumerable<AgentEvent> ReadChannelAsync(
        Channel<AgentEvent> channel, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (await channel.Reader.WaitToReadAsync(ct))
        {
            while (channel.Reader.TryRead(out var ev))
                yield return ev;
        }
    }
}
