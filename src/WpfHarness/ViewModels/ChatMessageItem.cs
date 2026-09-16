using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfHarness.ViewModels;

public enum ChatItemKind
{
    User,
    Assistant,
    ToolCall,
    Error
}

/// <summary>UI 层消息条目：从 Harness 事件流增量构建。</summary>
public partial class ChatMessageItem : ObservableObject
{
    public ChatItemKind Kind { get; init; }
    public DateTime Time { get; init; } = DateTime.Now;

    /// <summary>User 原文 / Assistant Markdown。</summary>
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    // ---- ToolCall 条目 ----
    [ObservableProperty]
    private string _toolName = string.Empty;

    [ObservableProperty]
    private string _toolArgs = string.Empty;

    [ObservableProperty]
    private string _toolResult = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isToolError;
}
