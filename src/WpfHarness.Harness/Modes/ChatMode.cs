using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Modes;

/// <summary>普通对话模式：单轮流式补全，无工具。</summary>
public sealed class ChatMode : IAgentMode
{
    public const string ModeId = "chat";

    public string Id => ModeId;
    public string DisplayName => "对话";
    public string Description => "单轮流式对话，不调用工具";

    public string BuildSystemPrompt(IToolRegistry tools) =>
        "你是 WpfHarness 的对话助手。用简洁、准确的中文回答用户问题。";

    public async IAsyncEnumerable<AgentEvent> RunAsync(
        AgentRunContext ctx, string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ctx.Session.Messages.Add(ChatMessage.FromUser(userInput));
        ctx.Emit(new TurnStarted(1));

        var request = new ChatRequest
        {
            Model = ctx.Model,
            Messages = ctx.Session.Messages.ToList(),
            Temperature = ctx.Options.Temperature,
            MaxTokens = ctx.Options.MaxTokens
        };

        ChatMessage? assistant = null;
        await foreach (var ev in ctx.Provider.StreamAsync(request, ct))
        {
            switch (ev)
            {
                case ProviderTextDelta delta:
                    (assistant ??= ChatMessage.FromAssistant(string.Empty)).Text += delta.Delta;
                    ctx.Emit(new TextDelta(delta.Delta));
                    break;
                case ProviderFinished finished:
                    if (assistant != null)
                    {
                        ctx.Session.Messages.Add(assistant);
                        ctx.Emit(new AssistantMessageCompleted(assistant, 1));
                    }
                    ctx.Session.UpdatedAt = DateTime.Now;
                    ctx.Emit(new AgentCompleted(finished.FinishReason));
                    break;
            }
        }
        yield break;
    }
}
