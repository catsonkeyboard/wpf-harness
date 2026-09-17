using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHarness.Views;

public class PreviewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
    string _inputText = "";
    public string Greeting { get; set; } = "夜深了，今天让 Harness 帮你做点什么？";
    public string InputText { get => _inputText; set { _inputText = value; Raise(); } }
    public List<object> Modes { get; } = new() {
        new { DisplayName = "对话" }, new { DisplayName = "ReAct" }, new { DisplayName = "ReAct（文本协议）" } };
    public List<object> Providers { get; } = new() { new { Name = "Mock（离线体验）" } };
    // 预置选中项，让离屏渲染与真实运行一致
    public object? SelectedMode { get; set; }
    public object? SelectedProvider { get; set; }
    public PreviewModel() { SelectedMode = Modes[1]; SelectedProvider = Providers[0]; }
}

public class SettingsPreviewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
    public System.Collections.ObjectModel.ObservableCollection<WpfHarness.Harness.ProviderConfig> Providers { get; } = new();
    public SettingsPreviewModel()
    {
        Providers.Add(new WpfHarness.Harness.ProviderConfig
        {
            Name = "GLM", Type = "openai-compat", BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini", ApiKey = "sk-***", IsDefault = true
        });
        Providers.Add(new WpfHarness.Harness.ProviderConfig
        {
            Name = "Mock", Type = "mock", BaseUrl = "local://mock", Model = "mock-1"
        });
    }
    public System.Windows.Input.ICommand RemoveProviderCommand { get; } = new SimpleCommand();
    public System.Windows.Input.ICommand AddProviderCommand { get; } = new SimpleCommand();
    class SimpleCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? p) => true;
        public void Execute(object? p) { }
    }
}

class LayoutCheck
{
    [STAThread]
    static void Main(string[] args)
    {
        var app = new WpfHarness.App();
        app.InitializeComponent();
        WpfHarness.App.ForceAccentPalette();
        var home = new HomeView { DataContext = new PreviewModel() };
        var width = args.Length > 1 ? double.Parse(args[1]) : 920.0;
        var height = args.Length > 2 ? double.Parse(args[2]) : 752.0;
        Render(home, args[0], width, height);
        Dump(home, home);
        Assert(home);
        Console.WriteLine("HOME PASS");

        var settings = new SettingsView { DataContext = new SettingsPreviewModel() };
        var sw = args.Length > 3 ? double.Parse(args[3]) : 640.0;
        Render(settings, Path.Combine(Path.GetDirectoryName(args[0])!, "settings.png"), sw, 560.0);
        Dump(settings, settings);
        AssertSettings(settings);
        DiagnoseAccent(settings, Path.Combine(Path.GetDirectoryName(args[0])!, "settings.png"));
        // 局部放大：核对下拉框边框是否真的画满整个 308 宽
        Zoom(Path.Combine(Path.GetDirectoryName(args[0])!, "settings.png"),
             Path.Combine(Path.GetDirectoryName(args[0])!, "settings-zoom.png"),
             new Int32Rect(130, 86, 350, 40), 3);
        Console.WriteLine("SETTINGS PASS");
    }

    /// <summary>裁剪并放大指定区域，便于肉眼核对细节。</summary>
    static void Zoom(string src, string dst, Int32Rect rect, int scale)
    {
        var frame = BitmapDecoder.Create(File.OpenRead(src), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        var cropped = new CroppedBitmap(frame, rect);
        var scaled = new TransformedBitmap(cropped, new ScaleTransform(scale, scale));
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(scaled));
        using var fs = File.Create(dst);
        enc.Save(fs);
    }

    // 强调色诊断：资源解析值 + 模板形状实际填充 + 截图像素采样
    static void DiagnoseAccent(Visual root, string png)
    {
        foreach (var k in new[] { "AccentBrush", "SystemAccentColorPrimaryBrush", "AccentFillColorDefaultBrush", "SystemAccentColorBrush" })
            Console.WriteLine($"RES {k} = {Application.Current.TryFindResource(k)}");
        var d = Application.Current.Resources;
        Console.WriteLine($"DIRECT AccentFillColorDefaultBrush: contains={d.Contains("AccentFillColorDefaultBrush")}, value={d["AccentFillColorDefaultBrush"]}");
        var radios = new List<Point>();
        Walk(root, root, o =>
        {
            if (o is RadioButton rb)
                radios.Add(((FrameworkElement)o).TransformToAncestor(root).Transform(new Point()));
            if (o is System.Windows.Shapes.Shape sh)
                Console.WriteLine($"Shape {sh.GetType().Name} Fill={sh.Fill} Stroke={sh.Stroke}");
        });
        var frame = BitmapDecoder.Create(File.OpenRead(png), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        int stride = frame.PixelWidth * 4;
        var px = new byte[stride * frame.PixelHeight];
        frame.CopyPixels(px, stride, 0);
        foreach (var c in radios)
        {
            string? best = null; double bestSat = -1;
            for (int dy = -14; dy <= 14; dy++)
                for (int dx = -14; dx <= 14; dx++)
                {
                    int x = (int)c.X + dy, y = (int)c.Y + dy; // scan diagonal band around center
                    int yy = (int)c.Y + dy; int xx = (int)c.X + dx;
                    if (xx < 0 || yy < 0 || xx >= frame.PixelWidth || yy >= frame.PixelHeight) continue;
                    int i = yy * stride + xx * 4;
                    int r = px[i + 2], g = px[i + 1], b = px[i]; // BGRA
                    int mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
                    double sat = mx <= 0 ? 0 : (mx - mn) / (double)mx;
                    if (sat > bestSat) { bestSat = sat; best = $"#{r:X2}{g:X2}{b:X2}"; }
                }
            Console.WriteLine($"Radio@({c.X:F0},{c.Y:F0}) 最饱和像素={best} (饱和度 {bestSat:F2})");
        }
    }
    // 设置页验收：输入框等宽、统一 32 高、卡片宽度一致
    static void AssertSettings(Visual root)
    {
        int fails = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {what}");
            if (!ok) fails++;
        }
        var boxes = new List<double>();
        var cardWidths = new List<double>();
        Walk(root, root, o =>
        {
            if (o is TextBox tb && tb.ActualWidth > 100 && tb.Style?.Setters.OfType<Setter>().Any(s => s.Property == FrameworkElement.HeightProperty) == true) boxes.Add(tb.ActualWidth);
            if (o is Border b && b.Name == "ProviderCard") cardWidths.Add(b.ActualWidth);
        });
        Check(boxes.Count >= 8, $"表单输入框 >= 8 个 (实际 {boxes.Count})");
        Check(boxes.All(w => Math.Abs(w - boxes.Max()) < 1), $"输入框等宽 (实际 {string.Join(",", boxes.Select(w => w.ToString("F0")))})");
        Check(cardWidths.Distinct().Count() == 1, $"Provider 卡片等宽 {string.Join(",", cardWidths.Select(w => w.ToString("F0")))}");
        if (fails > 0) Environment.Exit(1);
    }
    static void Render(FrameworkElement view, string path, double width, double height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(path)) encoder.Save(stream);
    }
    // 验收标准：640px 内容列居中；卡片不拉伸、不重叠；下拉框固定宽；建议卡两列对齐
    static void Assert(Visual root)
    {
        int fails = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {what}");
            if (!ok) fails++;
        }
        Border? card = null; ComboBox? combo = null; var cards = new List<double>(); var cardX = new List<double>();
        Walk(root, root, o =>
        {
            switch (o)
            {
                case Border b when b.Name == "InputCard": card = b; break;
                case ComboBox c: combo ??= c; break;
                case Button { ActualWidth: > 200 } btn: cards.Add(btn.ActualWidth); cardX.Add(((FrameworkElement)btn).TransformToAncestor(root).Transform(new Point()).X); break;
            }
        });
        var cardPos = card!.TransformToAncestor(root).Transform(new Point());
        Check(Math.Abs(card.ActualWidth - 640) < 1, $"输入卡宽度=640 (实际 {card.ActualWidth:F0})");
        Check(Math.Abs(cardPos.X - 140) < 1, $"输入卡水平居中 x=140 (实际 {cardPos.X:F0})");
        Check(combo!.ActualWidth <= 240, $"下拉框按内容自适应且不超过上限 240 (实际 {combo.ActualWidth:F0})");
        Check(cards.Count == 4, $"建议卡共 4 张 (实际 {cards.Count})");
        Check(cards.All(w => Math.Abs(w - 315) < 1), $"建议卡等宽 315 (实际 {string.Join(",", cards.Select(w => w.ToString("F0")))})");
        Check(Math.Abs(cardX[0] - cardX[2]) < 1 && Math.Abs(cardX[1] - cardX[3]) < 1, "建议卡两列垂直对齐");
        if (fails > 0) Environment.Exit(1);
    }
    static void Walk(DependencyObject node, Visual root, Action<object> visit)
    {
        if (node is Visual v && node is not System.Windows.Media.Geometry) visit(node);
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i), root, visit);
    }
    static void Dump(DependencyObject node, Visual root)
    {
        if (node is FrameworkElement e && (e is TextBox || e is ComboBox || e is Button || e is ToggleButton || e.Name == "InputCard" || e.Name == "Chrome"))
        {
            var pos = e.TransformToAncestor(root).Transform(new Point());
            var align = e is Control c && c.HorizontalAlignment != HorizontalAlignment.Stretch ? $" align={c.HorizontalAlignment}" : "";
            Console.WriteLine($"{e.GetType().Name} {e.Name}{(e.Name=="" && align=="" ? "" : "")}: {pos.X:F0},{pos.Y:F0} {e.ActualWidth:F0}x{e.ActualHeight:F0}{align}");
        }
        for (int i=0; i<VisualTreeHelper.GetChildrenCount(node); i++) Dump(VisualTreeHelper.GetChild(node,i),root);
    }
}
