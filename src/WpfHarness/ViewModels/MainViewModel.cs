using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfHarness.Harness;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;
using WpfHarness.Harness.Storage;

namespace WpfHarness.ViewModels;

public sealed record ModeInfo(string Id, string DisplayName);

public partial class MainViewModel : ObservableObject
{
    private readonly HarnessEngine _engine;
    private readonly AppConfigStore _configStore;
    private readonly JsonSessionStore _sessionStore;
    private CancellationTokenSource? _cts;

    public AppConfig Config { get; private set; }
    public ObservableCollection<ChatSessionViewModel> Sessions { get; } = new();
    public ObservableCollection<ModeInfo> Modes { get; }

    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = new();

    [ObservableProperty]
    private ProviderConfig? _selectedProvider;

    [ObservableProperty]
    private ModeInfo _selectedMode;

    [ObservableProperty]
    private ChatSessionViewModel? _selectedSession;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _greeting = string.Empty;

    public bool HasSession => SelectedSession != null;

    public event Action? SettingsRequested;

    public MainViewModel(HarnessEngine engine, AppConfigStore configStore, JsonSessionStore sessionStore)
    {
        _engine = engine;
        _configStore = configStore;
        _sessionStore = sessionStore;

        Config = configStore.Load();
        EnsureUsableDefaults();

        Modes = new ObservableCollection<ModeInfo>(
            engine.AvailableModes.Select(m => new ModeInfo(m.Id, m.DisplayName)));
        SelectedMode = Modes.FirstOrDefault(m => m.Id == Config.DefaultModeId) ?? Modes[0];

        SyncProviders();
        UpdateGreeting();

        foreach (var s in _sessionStore.LoadAll())
            Sessions.Add(new ChatSessionViewModel(s));
    }

    public void UpdateGreeting() => Greeting = DateTime.Now.Hour switch
    {
        < 6 => "夜深了，今天让 Harness 帮你做点什么？",
        < 11 => "早上好，今天让 Harness 帮你做点什么？",
        < 14 => "中午好，今天让 Harness 帮你做点什么？",
        < 18 => "下午好，今天让 Harness 帮你做点什么？",
        _ => "晚上好，今天让 Harness 帮你做点什么？"
    };

    private void EnsureUsableDefaults()
    {
        if (Config.Providers.Count == 0)
        {
            Config.Providers.Add(new ProviderConfig
            {
                Name = "Mock（离线体验）",
                Type = MockProvider.ProviderType,
                BaseUrl = "local://mock",
                Model = "mock-1"
            });
            Config.DefaultProviderId = Config.Providers[0].Id;
            _configStore.Save(Config);
        }
    }

    public void SyncProviders()
    {
        Providers = new ObservableCollection<ProviderConfig>(Config.Providers);
        SelectedProvider =
            Config.Providers.FirstOrDefault(p => p.Id == Config.DefaultProviderId) ?? Providers.FirstOrDefault();
    }

    [RelayCommand]
    private void NewTask()
    {
        SelectedSession = null;
        InputText = string.Empty;
    }

    [RelayCommand]
    private void DeleteSession(ChatSessionViewModel vm)
    {
        if (ReferenceEquals(SelectedSession, vm)) SelectedSession = null;
        Sessions.Remove(vm);
        _sessionStore.Delete(vm.Session.Id);
    }

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke();

    [RelayCommand]
    private void QuickPrompt(string prompt) => InputText = prompt;

    partial void OnSelectedSessionChanged(ChatSessionViewModel? value) =>
        OnPropertyChanged(nameof(HasSession));

    partial void OnSelectedModeChanged(ModeInfo value)
    {
        Config.DefaultModeId = value.Id;
        _configStore.Save(Config);
    }

    partial void OnSelectedProviderChanged(ProviderConfig? value)
    {
        if (value == null) return;
        Config.DefaultProviderId = value.Id;
        _configStore.Save(Config);
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsBusy) return;
        var text = InputText.Trim();
        if (text.Length == 0) return;
        var provider = SelectedProvider ?? throw new InvalidOperationException("请先在设置中配置模型服务");

        var vm = SelectedSession;
        if (vm == null)
        {
            var session = new Session
            {
                Title = Session.DeriveTitle(text),
                ModeId = SelectedMode.Id,
                ProviderId = provider.Id,
                Model = provider.Model,
            };
            vm = new ChatSessionViewModel(session);
            Sessions.Insert(0, vm);
            SelectedSession = vm;
        }

        InputText = string.Empty;
        IsBusy = true;
        vm.AppendUser(text);
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (var ev in _engine.RunAsync(vm.Session, provider, text,
                               options: null, _cts.Token))
            {
                vm.Apply(ev);
            }
        }
        catch (OperationCanceledException)
        {
            vm.Items.Add(new ChatMessageItem { Kind = ChatItemKind.Error, Text = "已停止。" });
        }
        catch (Exception ex)
        {
            vm.Items.Add(new ChatMessageItem { Kind = ChatItemKind.Error, Text = $"出错了：{ex.Message}" });
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsBusy = false;
            vm.IsBusy = false;
            vm.Session.UpdatedAt = DateTime.Now;
            _sessionStore.Save(vm.Session);
            vm.RebuildItems();
        }
    }
}
