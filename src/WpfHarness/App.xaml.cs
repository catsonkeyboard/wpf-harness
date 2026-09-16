using System.IO;

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WpfHarness.Harness;
using WpfHarness.Harness.Extensions;
using WpfHarness.Harness.Storage;
using WpfHarness.ViewModels;

namespace WpfHarness;

public partial class App : Application
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<AppConfigStore>();
            services.AddSingleton<JsonSessionStore>();
            services.AddSingleton(_ =>
                new HarnessBuilder()
                    .UseExtension(new BuiltinToolsExtension())
                    .Build());
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<Wpf.Ui.IContentDialogService, Wpf.Ui.ContentDialogService>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    public static T GetService<T>() where T : class =>
        _host.Services.GetService(typeof(T)) as T ?? throw new InvalidOperationException($"未注册服务: {typeof(T)}");

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();
        GetService<MainWindow>().Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
