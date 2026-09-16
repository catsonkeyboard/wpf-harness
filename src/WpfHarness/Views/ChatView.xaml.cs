using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfHarness.ViewModels;

namespace WpfHarness.Views;

public partial class ChatView
{
    private INotifyCollectionChanged? _hookedItems;

    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoadedOrChanged;
        DataContextChanged += (_, _) => OnLoadedOrChanged(this, EventArgs.Empty);
    }

    private void OnLoadedOrChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.PropertyChanged -= OnMainPropertyChanged;
        vm.PropertyChanged += OnMainPropertyChanged;
        HookSession(vm);
    }

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedSession) && sender is MainViewModel vm)
            HookSession(vm);
    }

    /// <summary>会话切换或新消息到达时自动滚到底部。</summary>
    private void HookSession(MainViewModel vm)
    {
        if (_hookedItems != null)
            _hookedItems.CollectionChanged -= OnItemsChanged;
        _hookedItems = vm.SelectedSession?.Items;
        if (_hookedItems != null)
            _hookedItems.CollectionChanged += OnItemsChanged;
        if (_hookedItems != null) MessagesScroll.ScrollToEnd();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.BeginInvoke(() => MessagesScroll.ScrollToEnd());

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;
        if (DataContext is MainViewModel vm && vm.SendCommand.CanExecute(null))
        {
            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }
}
