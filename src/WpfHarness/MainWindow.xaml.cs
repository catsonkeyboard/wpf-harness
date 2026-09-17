using System.Windows;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Controls;
using WpfHarness.Harness.Storage;
using WpfHarness.ViewModels;
using WpfHarness.Views;

namespace WpfHarness;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _mainViewModel;

    public MainWindow(MainViewModel viewModel, IContentDialogService dialogService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _mainViewModel = viewModel;

        dialogService.SetDialogHost(RootContentDialog);
        viewModel.SettingsRequested += ShowSettingsDialog;
    }

    private async void ShowSettingsDialog()
    {
        var store = App.GetService<AppConfigStore>();
        var settingsVm = new SettingsViewModel(store, _mainViewModel.Config);
        var view = new SettingsView { DataContext = settingsVm };

        var dialog = new ContentDialog(App.GetService<IContentDialogService>().GetDialogHostEx())
        {
            Title = "设置 · 模型服务",
            Content = view,
            PrimaryButtonText = "保存",
            SecondaryButtonText = "取消",
            CloseButtonText = string.Empty,
            DialogMaxWidth = 640
        };
        // WPF-UI 运行时会把控件强调色重置为系统紫（应用级资源覆盖无效）。
        // 在对话框自身资源层写入橙色：资源解析时控件层级优先于应用级主题，确定性生效。
        dialog.Resources["AccentFillColorDefaultBrush"] = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
        dialog.Resources["SystemAccentColorPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
        dialog.Resources["SystemAccentColorBrush"] = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
        dialog.Resources["TextOnAccentFillColorPrimaryBrush"] = Brushes.White;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            settingsVm.Save();
            _mainViewModel.SyncProviders();
        }
    }
}
