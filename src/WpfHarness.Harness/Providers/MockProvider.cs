using System.Text.Json;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness;

/// <summary>
/// 离线 Mock Provider：无 Key 也能走通全部链路。
/// 无工具时逐词回显；带工具时先调用第一个工具一次，再基于结果生成最终回答。
/// </summary>
public sealed class MockProvider : IChatProvider
{
    public const string ProviderType = "mock";

    private readonly ProviderConfig _config;

    public MockProvider(ProviderConfig config) => _config = config;

    public string Id => _config.Id;

    public async IAsyncEnumerable<ProviderEvent> StreamAsync(
        ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var lastUser = request.Messages.LastOrDefault(m => m.Role == MessageRole.User)?.Text ?? "";
        var hasToolResult = request.Messages.Any(m => m.Role == MessageRole.Tool);

        if (request.Tools.Count > 0 && !hasToolResult)
        {
            // 演示 ReAct：先输出 Thought，再发起一次工具调用
            yield return new ProviderTextDelta($"Thought: 我需要先调用 {request.Tools[0].Name} 来获取信息。\n");
            var call = new ToolCall
            {
                Id = "mock_" + Guid.NewGuid().ToString("N")[..8],
                Name = request.Tools[0].Name,
                ArgumentsJson = "{}"
            };
            yield return new ProviderToolCall(call);
            yield return new ProviderFinished("tool_calls");
            yield break;
        }

        var toolResult = request.Messages.LastOrDefault(m => m.Role == MessageRole.Tool)?.Text;
        var reply = toolResult == null
            ? $"[Mock:{request.Model}] Echo: {lastUser}"
            : $"[Mock:{request.Model}] 我调用了工具并拿到结果：{toolResult}";

        foreach (var word in reply.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            await Task.Delay(40, ct);
            yield return new ProviderTextDelta(word + " ");
        }
        yield return new ProviderFinished("stop");
    }
}

public sealed class OpenAiCompatProviderFactory : IChatProviderFactory
{
    public string Type => OpenAiCompatProvider.ProviderType;
    public IChatProvider Create(ProviderConfig config) => new OpenAiCompatProvider(config);
}

public sealed class MockProviderFactory : IChatProviderFactory
{
    public string Type => MockProvider.ProviderType;
    public IChatProvider Create(ProviderConfig config) => new MockProvider(config);
}
