using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Abstractions;

/// <summary>一次 Agent 运行的上下文：会话 + 依赖 + 事件出口。</summary>
public sealed class AgentRunContext
{
    public required Session Session { get; init; }
    public required IChatProvider Provider { get; init; }
    public required string Model { get; init; }
    public required IToolRegistry Tools { get; init; }
    public required AgentOptions Options { get; init; }

    /// <summary>模式内广播事件（由 Engine 注入，最终流向调用方与扩展）。</summary>
    public required Action<AgentEvent> Emit { get; init; }
}

/// <summary>
/// Agent 运行模式（可插拔）：Chat、ReAct（原生工具调用）、ReAct 文本协议等。
/// 模式负责整个推理循环，并把过程事件通过 ctx.Emit 广播。
/// </summary>
public interface IAgentMode
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }

    /// <summary>默认系统提示词（可被 Options/调用方覆盖）。</summary>
    string BuildSystemPrompt(IToolRegistry tools);

    IAsyncEnumerable<AgentEvent> RunAsync(AgentRunContext ctx, string userInput, CancellationToken ct = default);
}
