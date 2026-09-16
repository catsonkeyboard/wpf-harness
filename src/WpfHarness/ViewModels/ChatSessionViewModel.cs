using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.ViewModels;

/// <summary>一个会话的 UI 状态：把 Harness 事件流投影为可绑定的消息条目。</summary>
public partial class ChatSessionViewModel : ObservableObject
{
    private ChatMessageItem? _streamingAssistant;

    public Session Session { get; }
    public ObservableCollection<ChatMessageItem> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public string Title => Session.Title;
    public string ModelDisplay => Session.Model;
    public DateTime UpdatedAt => Session.UpdatedAt;

    public ChatSessionViewModel(Session session)
    {
        Session = session;
        RebuildItems();
    }

    public void RebuildItems()
    {
        Items.Clear();
        _streamingAssistant = null;
        foreach (var m in Session.Messages)
        {
            switch (m.Role)
            {
                case MessageRole.User when m.Text?.StartsWith("Observation:") != true:
                    Items.Add(new ChatMessageItem { Kind = ChatItemKind.User, Text = m.Text ?? "" });
                    break;
                case MessageRole.User: // ReAct 文本协议的 Observation 回填
                    AttachObservation(m.Text!);
                    break;
                case MessageRole.Assistant:
                    Items.Add(new ChatMessageItem
                    {
                        Kind = ChatItemKind.Assistant,
                        Text = m.Text ?? string.Empty
                    });
                    foreach (var call in m.ToolCalls)
                        Items.Add(new ChatMessageItem
                        {
                            Kind = ChatItemKind.ToolCall,
                            ToolName = call.Name,
                            ToolArgs = call.ArgumentsJson
                        });
                    break;
                case MessageRole.Tool:
                    AttachObservation(m.Text ?? "", m.ToolName, m.IsError);
                    break;
            }
        }
    }

    private void AttachObservation(string text, string? toolName = null, bool isError = false)
    {
        // 找最近一个尚未有结果的 ToolCall 条目
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i].Kind == ChatItemKind.ToolCall && string.IsNullOrEmpty(Items[i].ToolResult))
            {
                Items[i].ToolResult = text;
                Items[i].IsToolError = isError;
                Items[i].IsRunning = false;
                return;
            }
        }
        Items.Add(new ChatMessageItem
        {
            Kind = ChatItemKind.ToolCall,
            ToolName = toolName ?? "tool",
            ToolResult = text,
            IsToolError = isError
        });
    }

    public void AppendUser(string text) =>
        Items.Add(new ChatMessageItem { Kind = ChatItemKind.User, Text = text });

    /// <summary>消费引擎事件（UI 线程调用）。</summary>
    public void Apply(AgentEvent ev)
    {
        switch (ev)
        {
            case TurnStarted:
                _streamingAssistant = null;
                IsBusy = true;
                break;
            case TextDelta delta:
                _streamingAssistant ??= NewStreamingAssistant();
                _streamingAssistant.Text += delta.Delta;
                break;
            case AssistantMessageCompleted:
                if (_streamingAssistant != null) _streamingAssistant.IsStreaming = false;
                _streamingAssistant = null;
                break;
            case ToolCallStarted started:
                _streamingAssistant = null;
                Items.Add(new ChatMessageItem
                {
                    Kind = ChatItemKind.ToolCall,
                    ToolName = started.Call.Name,
                    ToolArgs = started.Call.ArgumentsJson,
                    IsRunning = true
                });
                break;
            case ToolCallFinished finished:
                for (var i = Items.Count - 1; i >= 0; i--)
                {
                    if (Items[i].Kind == ChatItemKind.ToolCall && Items[i].IsRunning)
                    {
                        Items[i].ToolResult = finished.Result;
                        Items[i].IsToolError = finished.IsError;
                        Items[i].IsRunning = false;
                        break;
                    }
                }
                break;
            case AgentCompleted:
                IsBusy = false;
                _streamingAssistant = null;
                break;
            case AgentFailed failed:
                IsBusy = false;
                _streamingAssistant = null;
                Items.Add(new ChatMessageItem
                {
                    Kind = ChatItemKind.Error,
                    Text = failed.Error.Message
                });
                break;
        }
    }

    private ChatMessageItem NewStreamingAssistant()
    {
        var item = new ChatMessageItem { Kind = ChatItemKind.Assistant, IsStreaming = true };
        Items.Add(item);
        return item;
    }
}
