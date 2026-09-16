using System.Windows;
using System.Windows.Controls;

namespace WpfHarness.ViewModels;

/// <summary>按消息条目类型挑选模板。</summary>
public sealed class ChatItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTemplate { get; set; }
    public DataTemplate? AssistantTemplate { get; set; }
    public DataTemplate? ToolCallTemplate { get; set; }
    public DataTemplate? ErrorTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container) =>
        item switch
        {
            ChatMessageItem { Kind: ChatItemKind.User } => UserTemplate,
            ChatMessageItem { Kind: ChatItemKind.Assistant } => AssistantTemplate,
            ChatMessageItem { Kind: ChatItemKind.ToolCall } => ToolCallTemplate,
            ChatMessageItem { Kind: ChatItemKind.Error } => ErrorTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
