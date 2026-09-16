using System.Windows;
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
            DialogMaxWidth = 640
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            settingsVm.Save();
            _mainViewModel.SyncProviders();
        }
    }
}
