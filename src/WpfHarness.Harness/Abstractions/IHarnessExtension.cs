namespace WpfHarness.Harness.Abstractions;

/// <summary>
/// 扩展点（pi 风格）：扩展在构建期通过 HarnessBuilder 注入工具、模式、Provider，
/// 在运行期订阅事件流。内置工具即以扩展形式注册，作为示例。
/// </summary>
public interface IHarnessExtension
{
    string Id { get; }

    void Configure(HarnessBuilder builder);
}

/// <summary>运行期事件订阅器：扩展可观察所有 Agent 事件（审计、日志、遥测等）。</summary>
public interface IAgentEventSubscriber
{
    void OnEvent(string sessionId, AgentEvent @event);
}
