using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;

namespace WpfHarness.Harness;

/// <summary>
/// OpenAI 兼容 Chat Completions Provider（OpenAI / GLM / DeepSeek / Ollama / vLLM 等皆可）。
/// 流式 SSE 解析，聚合增量 tool_calls 后再向上抛 ProviderToolCall。
/// </summary>
public sealed class OpenAiCompatProvider : IChatProvider
{
    public const string ProviderType = "openai-compat";

    private readonly ProviderConfig _config;
    private readonly HttpClient _http;

    public OpenAiCompatProvider(ProviderConfig config)
    {
        _config = config;
        var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) };
        if (!string.IsNullOrWhiteSpace(config.Proxy) && Uri.TryCreate(config.Proxy, UriKind.Absolute, out var proxyUri))
        {
            handler.Proxy = new System.Net.WebProxy(proxyUri);
            handler.UseProxy = true;
        }
        _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public string Id => _config.Id;

    public async IAsyncEnumerable<ProviderEvent> StreamAsync(
        ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}/chat/completions";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrEmpty(_config.ApiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Content = new StringContent(BuildRequestBody(request), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var textBuf = new StringBuilder();
        // index -> 聚合中的 tool call
        var toolBuf = new Dictionary<int, ToolCall>();
        string? finishReason = null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith(':')) continue;
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;

            using var doc = JsonDocument.Parse(data);
            var delta = doc.RootElement
                .TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                    ? choices[0].TryGetProperty("delta", out var d) ? d : (JsonElement?)null
                    : null;
            if (delta == null) continue;

            if (delta.Value.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            {
                var fragment = content.GetString();
                if (!string.IsNullOrEmpty(fragment))
                {
                    textBuf.Append(fragment);
                    yield return new ProviderTextDelta(fragment);
                }
            }

            if (delta.Value.TryGetProperty("tool_calls", out var toolCalls) &&
                toolCalls.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in toolCalls.EnumerateArray())
                {
                    var index = tc.TryGetProperty("index", out var ix) ? ix.GetInt32() : 0;
                    if (!toolBuf.TryGetValue(index, out var agg))
                        toolBuf[index] = agg = new ToolCall();
                    if (tc.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        agg.Id = id.GetString()!;
                    if (tc.TryGetProperty("function", out var fn))
                    {
                        if (fn.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                            agg.Name += name.GetString();
                        if (fn.TryGetProperty("arguments", out var args) && args.ValueKind == JsonValueKind.String)
                            agg.ArgumentsJson += args.GetString();
                    }
                }
            }

            if (choices[0].TryGetProperty("finish_reason", out var fr) &&
                fr.ValueKind == JsonValueKind.String && fr.GetString() is { } reason)
            {
                finishReason = reason;
            }
        }

        foreach (var call in toolBuf.Values.Where(c => !string.IsNullOrEmpty(c.Name)))
            yield return new ProviderToolCall(call);

        yield return new ProviderFinished(finishReason);
    }

    private string BuildRequestBody(ChatRequest request)
    {
        var messages = new List<object>();
        foreach (var m in request.Messages)
        {
            switch (m.Role)
            {
                case MessageRole.System:
                    messages.Add(new { role = "system", content = m.Text ?? "" });
                    break;
                case MessageRole.User:
                    messages.Add(new { role = "user", content = m.Text ?? "" });
                    break;
                case MessageRole.Assistant:
                    messages.Add(new
                    {
                        role = "assistant",
                        content = m.Text ?? "",
                        tool_calls = m.ToolCalls.Count == 0
                            ? null
                            : m.ToolCalls.Select(c => new
                            {
                                id = c.Id,
                                type = "function",
                                function = new { name = c.Name, arguments = c.ArgumentsJson }
                            }).ToList()
                    });
                    break;
                case MessageRole.Tool:
                    messages.Add(new
                    {
                        role = "tool",
                        tool_call_id = m.ToolCallId ?? "",
                        name = m.ToolName ?? "",
                        content = m.Text ?? ""
                    });
                    break;
            }
        }

        var body = new
        {
            model = request.Model,
            messages,
            stream = true,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            tools = request.Tools.Count == 0
                ? null
                : request.Tools.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = JsonNodeFromSchema(t.ParametersJsonSchema)
                    }
                }).ToList()
        };
        return JsonSerializer.Serialize(body);
    }

    private static object? JsonNodeFromSchema(string schemaJson)
    {
        try
        {
            return System.Text.Json.Nodes.JsonNode.Parse(schemaJson);
        }
        catch (JsonException)
        {
            return new { type = "object", properties = new { } };
        }
    }
}
