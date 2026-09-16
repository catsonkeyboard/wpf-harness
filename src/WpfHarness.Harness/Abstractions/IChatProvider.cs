using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Abstractions;

/// <summary>Provider 向上抛出的流式事件（已聚合好工具调用）。</summary>
public abstract record ProviderEvent;

public sealed record ProviderTextDelta(string Delta) : ProviderEvent;

public sealed record ProviderToolCall(ToolCall Call) : ProviderEvent;

public sealed record ProviderFinished(string? FinishReason) : ProviderEvent;

/// <summary>
/// LLM 接入抽象：任何能以流式方式补全对话的服务都可以实现此接口。
/// 实现方负责把统一消息模型转换为该服务的线上格式，并把 SSE 聚合为事件。
/// </summary>
public interface IChatProvider
{
    string Id { get; }

    IAsyncEnumerable<ProviderEvent> StreamAsync(ChatRequest request, CancellationToken ct = default);
}

/// <summary>Provider 工厂：按配置中的 Type 创建 Provider 实例，向 HarnessEngine 注册。</summary>
public interface IChatProviderFactory
{
    /// <summary>配置里的 type 标识，如 "openai-compat"、"mock"。</summary>
    string Type { get; }

    IChatProvider Create(ProviderConfig config);
}
