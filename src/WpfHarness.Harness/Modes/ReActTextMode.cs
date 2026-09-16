using System.Text;
using System.Text.RegularExpressions;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness.Modes;

/// <summary>
/// ReAct 文本协议模式：不依赖原生 function-calling，
/// 通过提示词约定 "Thought/Action/Args/Final Answer" 文本协议，适合任意补全模型。
/// </summary>
public sealed partial class ReActTextMode : IAgentMode
{
    public const string ModeId = "react-text";

    public string Id => ModeId;
    public string DisplayName => "ReAct（文本协议）";
    public string Description => "纯文本 Thought/Action/Observation 协议，兼容任意模型";

    [GeneratedRegex(@"Action:\s*(?<name>[\w.-]+)\s*\n\s*Args:\s*(?<args>\{.*?\})", RegexOptions.Singleline)]
    private static partial Regex ActionRegex();

    public string BuildSystemPrompt(IToolRegistry tools)
    {
        var sb = new StringBuilder("""
            你是一个 ReAct 智能体，只能使用以下文本协议回复，除此之外不要输出任何其他格式：

            Thought: <你的分析>
            Action: <工具名>
            Args: <JSON 参数>

            当你掌握足够信息时，改用：

            Thought: <你的分析>
            Final Answer: <最终答案>

            可用工具：
            """);
        foreach (var t in tools.Tools)
            sb.AppendLine($"- {t.Name}: {t.Description}");
        sb.AppendLine();

        sb.AppendLine("""
            之前轮次的工具结果会以 "Observation: <结果>" 的形式附在对话中。
            每次回复只能包含一个 Action 或一个 Final Answer。
            """);
        return sb.ToString();
    }

    public async IAsyncEnumerable<AgentEvent> RunAsync(
        AgentRunContext ctx, string userInput, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ctx.Session.Messages.Insert(0, ChatMessage.SystemPrompt(BuildSystemPrompt(ctx.Tools)));
        ctx.Session.Messages.Add(ChatMessage.FromUser(userInput));

        for (var turn = 1; turn <= ctx.Options.MaxIterations; turn++)
        {
            ctx.Emit(new TurnStarted(turn));
            var request = new ChatRequest
            {
                Model = ctx.Model,
                Messages = ctx.Session.Messages.ToList(),
                Tools = ctx.Options.EnableTools ? ctx.Tools.Tools.ToList() : new List<ToolDef>(),
                Temperature = ctx.Options.Temperature,
                MaxTokens = ctx.Options.MaxTokens
            };

            var text = new StringBuilder();
            await foreach (var ev in ctx.Provider.StreamAsync(request, ct))
            {
                if (ev is ProviderTextDelta delta)
                {
                    text.Append(delta.Delta);
                    ctx.Emit(new TextDelta(delta.Delta));
                }
            }

            var reply = text.ToString().Trim();
            var match = ActionRegex().Match(reply);
            var assistant = ChatMessage.FromAssistant(reply);
            ctx.Session.Messages.Add(assistant);
            ctx.Emit(new AssistantMessageCompleted(assistant, turn));

            if (!match.Success)
            {
                ctx.Session.UpdatedAt = DateTime.Now;
                ctx.Emit(new AgentCompleted("final_answer"));
                yield break;
            }

            var name = match.Groups["name"].Value;
            var args = match.Groups["args"].Value;
            var call = new ToolCall { Id = $"text_{turn}_{Guid.NewGuid().ToString("N")[..6]}", Name = name, ArgumentsJson = args };
            ctx.Emit(new ToolCallStarted(call));

            string result;
            bool isError;
            if (ctx.Tools.Find(name) is null)
            {
                result = $"工具 {name} 不存在";
                isError = true;
            }
            else
            {
                try { (result, isError) = await ctx.Tools.ExecuteAsync(name, args, ct); }
                catch (Exception ex) { result = ex.Message; isError = true; }
            }

            ctx.Session.Messages.Add(ChatMessage.FromUser($"Observation: {result}"));
            ctx.Emit(new ToolCallFinished(call, result, isError));
        }

        ctx.Session.UpdatedAt = DateTime.Now;
        ctx.Emit(new AgentCompleted("max_iterations"));
    }
}
