using System.IO;

using System.Windows;
using System.Windows.Media;
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
        // WPF-UI 主题管理器会在运行时重置控件强调色（覆盖 XAML 里的静态资源），
        // 因此在主题应用之后用代码强制写回橙色体系。
        ForceAccentPalette();
        await _host.StartAsync();
        GetService<MainWindow>().Show();
    }

    /// <summary>
    /// 把 WPF-UI 的控件强调色键尽量覆盖为橙色。
    /// 注意（实测）：WPF-UI 运行时主题仍可能把部分键（如 AccentFillColorDefaultBrush）
    /// 写回系统紫。因此依赖它的地方应优先用**自定义控件模板**或在控件自身 Resources 上覆盖，
    /// 不要指望本方法一劳永逸。
    /// </summary>
    public static void ForceAccentPalette()
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var patch = new ResourceDictionary();
        void Brush(string key, byte r, byte g, byte b) =>
            patch[key] = new SolidColorBrush(Color.FromRgb(r, g, b));
        Brush("SystemAccentColor", 0xF9, 0x73, 0x16);
        Brush("SystemAccentColorPrimary", 0xF9, 0x73, 0x16);
        Brush("SystemAccentColorSecondary", 0xFB, 0x8C, 0x3C);
        Brush("SystemAccentColorTertiary", 0xC2, 0x64, 0x30);
        Brush("SystemAccentColorBrush", 0xF9, 0x73, 0x16);
        Brush("SystemAccentColorPrimaryBrush", 0xF9, 0x73, 0x16);
        Brush("SystemAccentColorSecondaryBrush", 0xFB, 0x8C, 0x3C);
        Brush("SystemAccentColorTertiaryBrush", 0xC2, 0x64, 0x30);
        Brush("AccentFillColorDefaultBrush", 0xF9, 0x73, 0x16);
        Brush("AccentFillColorSecondaryBrush", 0xFB, 0x8C, 0x3C);
        Brush("AccentFillColorTertiaryBrush", 0xC2, 0x64, 0x30);
        Brush("AccentTextFillColorPrimaryBrush", 0xFB, 0x8C, 0x3C);
        Brush("TextOnAccentFillColorPrimaryBrush", 0xFF, 0xFF, 0xFF);
        // 同时写入 Application.Resources（层级高于合并字典）与 MergedDictionaries 首位。
        foreach (var key in patch.Keys)
            Application.Current.Resources[key] = patch[key];
        dicts.Insert(0, patch);
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
