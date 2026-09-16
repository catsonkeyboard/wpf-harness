namespace WpfHarness.Harness;

/// <summary>一个 LLM 接入点的配置。Type 决定用哪个 ProviderFactory。</summary>
public sealed class ProviderConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新模型";
    public string Type { get; set; } = OpenAiCompatProvider.ProviderType;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string? Proxy { get; set; }

    /// <summary>密文存储（机器绑定加密，见 SecretProtector）。</summary>
    public string? ApiKeyEncrypted { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>仅设置界面使用：是否为默认 Provider，保存时映射回 DefaultProviderId。</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDefault { get; set; }
}

public sealed class AppConfig
{
    public List<ProviderConfig> Providers { get; set; } = new();
    public string DefaultProviderId { get; set; } = string.Empty;
    public string DefaultModeId { get; set; } = "chat";

    public ProviderConfig? DefaultProvider =>
        Providers.FirstOrDefault(p => p.Id == DefaultProviderId) ?? Providers.FirstOrDefault();
}
