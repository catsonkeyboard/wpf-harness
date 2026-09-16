using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfHarness.Harness;
using WpfHarness.Harness.Storage;

namespace WpfHarness.ViewModels;

/// <summary>设置对话框 VM：编辑 Provider 列表副本，保存时写回 AppConfig。</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppConfigStore _store;
    private readonly AppConfig _config;

    public ObservableCollection<ProviderConfig> Providers { get; } = new();

    public SettingsViewModel(AppConfigStore store, AppConfig config)
    {
        _store = store;
        _config = config;
        foreach (var p in config.Providers)
        {
            p.IsDefault = p.Id == config.DefaultProviderId;
            Providers.Add(p);
        }
    }

    [RelayCommand]
    private void AddProvider() => Providers.Add(new ProviderConfig
    {
        Name = "新模型",
        Type = OpenAiCompatProvider.ProviderType,
        BaseUrl = "https://api.openai.com/v1",
        Model = "gpt-4o-mini"
    });

    [RelayCommand]
    private void RemoveProvider(ProviderConfig provider) => Providers.Remove(provider);

    /// <summary>写回配置并持久化（IsDefault 映射回 DefaultProviderId）。</summary>
    public void Save()
    {
        _config.Providers = Providers.ToList();
        _config.DefaultProviderId =
            Providers.FirstOrDefault(p => p.IsDefault)?.Id ?? Providers.FirstOrDefault()?.Id ?? string.Empty;
        _store.Save(_config);
    }
}
