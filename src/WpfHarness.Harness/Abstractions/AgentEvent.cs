using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Abstractions;

/// <summary>
/// Agent 运行期间对外广播的事件流（pi 风格）：UI 或扩展订阅这些事件即可增量渲染。
/// </summary>
public abstract record AgentEvent;

public sealed record TurnStarted(int Turn) : AgentEvent;

/// <summary>助手输出的文本增量（含 ReAct 的 Thought）。</summary>
public sealed record TextDelta(string Delta) : AgentEvent;

/// <summary>一轮助手消息落定（已写入会话）。</summary>
public sealed record AssistantMessageCompleted(ChatMessage Message, int Turn) : AgentEvent;

public sealed record ToolCallStarted(ToolCall Call) : AgentEvent;

public sealed record ToolCallFinished(ToolCall Call, string Result, bool IsError) : AgentEvent;

public sealed record AgentCompleted(string? FinishReason) : AgentEvent;

public sealed record AgentFailed(Exception Error) : AgentEvent;
