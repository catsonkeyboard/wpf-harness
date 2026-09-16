namespace WpfHarness.Harness.Models;

using WpfHarness.Harness.Abstractions;

public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool
}

/// <summary>模型请求的工具调用（原生 function-calling 格式）。</summary>
public sealed class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";

    public ToolCall Clone() => new() { Id = Id, Name = Name, ArgumentsJson = ArgumentsJson };
}

/// <summary>
/// 统一消息模型。文本、工具调用、工具结果均为一等公民，
/// 由 Provider 负责与各 LLM 的线上格式互转。
/// </summary>
public sealed class ChatMessage
{
    public MessageRole Role { get; set; }
    public string? Text { get; set; }
    public List<ToolCall> ToolCalls { get; set; } = new();

    /// <summary>Role == Tool 时，对应的调用 Id。</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Role == Tool 时，工具名。</summary>
    public string? ToolName { get; set; }

    public bool IsError { get; set; }

    public static ChatMessage SystemPrompt(string text) => new() { Role = MessageRole.System, Text = text };
    public static ChatMessage FromUser(string text) => new() { Role = MessageRole.User, Text = text };
    public static ChatMessage FromAssistant(string? text, List<ToolCall>? calls = null) =>
        new() { Role = MessageRole.Assistant, Text = text, ToolCalls = calls ?? new() };
    public static ChatMessage FromTool(string toolCallId, string toolName, string result, bool isError = false) =>
        new() { Role = MessageRole.Tool, ToolCallId = toolCallId, ToolName = toolName, Text = result, IsError = isError };
}

public sealed class ChatRequest
{
    public string Model { get; set; } = string.Empty;
    public IList<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public IList<ToolDef> Tools { get; set; } = new List<ToolDef>();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

public sealed class AgentOptions
{
    /// <summary>ReAct 循环最大轮数，防止死循环。</summary>
    public int MaxIterations { get; set; } = 6;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public bool EnableTools { get; set; } = true;
    public TimeSpan? RequestTimeout { get; set; }
}
