using System.Text;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Modes;

/// <summary>
/// ReAct 模式（原生 function-calling）：
/// Thought（文本流）→ Action（工具调用）→ Observation（工具结果）循环，直至 Final Answer。
/// 工具调用由模型通过 tool_calls 字段发起，协议由 OpenAI 兼容层保证。
/// </summary>
public sealed class ReActMode : IAgentMode
{
    public const string ModeId = "react";

    public string Id => ModeId;
    public string DisplayName => "ReAct";
    public string Description => "推理 + 工具调用循环（原生 function calling）";

    public string BuildSystemPrompt(IToolRegistry tools)
    {
        var sb = new StringBuilder("""
            你是一个 ReAct 智能体。按以下循环解决用户问题：
            1. Thought：分析当前状态，判断下一步；
            2. 若需要外部信息或操作，调用合适的工具（Action）；
            3. 收到 Observation（工具结果）后继续思考；
            4. 信息足够时，给出最终回答（Final Answer），直接输出答案文本。

            规则：
            - 每轮最多发起必要的工具调用，不要重复已成功的调用；
            - 工具参数必须是合法 JSON；
            - 最终回答使用简洁中文。

            可用工具：
            """);
        foreach (var t in tools.Tools)
            sb.AppendLine($"- {t.Name}: {t.Description}");
        return sb.ToString();
    }

    public async IAsyncEnumerable<AgentEvent> RunAsync(
        AgentRunContext ctx, string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var isFirstTurn = !ctx.Session.Messages.Any(m => m.Role == MessageRole.User);
        if (isFirstTurn)
            ctx.Session.Messages.Insert(0, ChatMessage.SystemPrompt(BuildSystemPrompt(ctx.Tools)));
        ctx.Session.Messages.Add(ChatMessage.FromUser(userInput));

        var tools = ctx.Options.EnableTools ? ctx.Tools.Tools.ToList() : new List<ToolDef>();

        for (var turn = 1; turn <= ctx.Options.MaxIterations; turn++)
        {
            ctx.Emit(new TurnStarted(turn));
            var request = new ChatRequest
            {
                Model = ctx.Model,
                Messages = ctx.Session.Messages.ToList(),
                Tools = tools,
                Temperature = ctx.Options.Temperature,
                MaxTokens = ctx.Options.MaxTokens
            };

            var text = new StringBuilder();
            var calls = new List<ToolCall>();
            string? finishReason = null;

            await foreach (var ev in ctx.Provider.StreamAsync(request, ct))
            {
                switch (ev)
                {
                    case ProviderTextDelta delta:
                        text.Append(delta.Delta);
                        ctx.Emit(new TextDelta(delta.Delta));
                        break;
                    case ProviderToolCall call:
                        calls.Add(call.Call);
                        break;
                    case ProviderFinished finished:
                        finishReason = finished.FinishReason;
                        break;
                }
            }

            var assistant = ChatMessage.FromAssistant(text.ToString(), calls);
            ctx.Session.Messages.Add(assistant);
            ctx.Emit(new AssistantMessageCompleted(assistant, turn));

            if (calls.Count == 0)
            {
                ctx.Session.UpdatedAt = DateTime.Now;
                ctx.Emit(new AgentCompleted(finishReason));
                yield break;
            }

            foreach (var call in calls)
            {
                ctx.Emit(new ToolCallStarted(call));
                string result;
                bool isError;
                if (ctx.Tools.Find(call.Name) is null)
                {
                    result = $"工具 {call.Name} 不存在";
                    isError = true;
                }
                else
                {
                    try
                    {
                        (result, isError) = await ctx.Tools.ExecuteAsync(call.Name, call.ArgumentsJson, ct);
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        isError = true;
                    }
                }
                ctx.Session.Messages.Add(ChatMessage.FromTool(call.Id, call.Name, result, isError));
                ctx.Emit(new ToolCallFinished(call, result, isError));
            }
        }

        ctx.Session.Messages.Add(ChatMessage.FromAssistant("已达到最大推理轮数，停止继续调用工具。"));
        ctx.Session.UpdatedAt = DateTime.Now;
        ctx.Emit(new AgentCompleted("max_iterations"));
    }
}
