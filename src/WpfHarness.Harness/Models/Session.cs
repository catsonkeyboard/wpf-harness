using System.Text.Json.Serialization;

namespace WpfHarness.Harness.Models;

public sealed class Session
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "新任务";
    public string ModeId { get; set; } = "chat";
    public string ProviderId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonIgnore]
    public string Preview
    {
        get
        {
            var first = Messages.FirstOrDefault(m => m.Role == MessageRole.User)?.Text;
            if (string.IsNullOrWhiteSpace(first)) return Title;
            return first.Length <= 40 ? first : first[..40] + "…";
        }
    }

    public static string DeriveTitle(string userInput)
    {
        var t = userInput.Replace("\r", " ").Replace("\n", " ").Trim();
        if (t.Length <= 18) return string.IsNullOrEmpty(t) ? "新任务" : t;
        return t[..18] + "…";
    }
}
