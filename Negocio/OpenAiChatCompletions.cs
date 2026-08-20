using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResimamisBackend.Negocio;

internal sealed class OpenAiChatCompletions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory httpClientFactory;

    public OpenAiChatCompletions(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<OpenAiChatResponse> CompletarAsync(
        string apiKey,
        OpenAiChatRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("OpenAI");
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(
            httpRequest,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detalle = ExtraerErrorOpenAi(body);
            throw new ApplicationException(
                $"Error al consultar OpenAI ({(int)response.StatusCode}): {detalle}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body, JsonOptions);
        if (parsed?.Choices == null || parsed.Choices.Count == 0)
            throw new ApplicationException("OpenAI no devolvió una respuesta.");
        return parsed;
    }

    private static string ExtraerErrorOpenAi(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
                return message.GetString() ?? body;
        }
        catch (JsonException)
        {
        }

        return string.IsNullOrWhiteSpace(body) ? "sin detalle" : body.Length > 400 ? body[..400] : body;
    }
}

internal sealed class OpenAiChatRequest
{
    public string Model { get; set; } = "";
    public List<OpenAiMessage> Messages { get; set; } = new();
    public List<OpenAiTool>? Tools { get; set; }
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
}

internal sealed class OpenAiMessage
{
    public string Role { get; set; } = "";
    public string? Content { get; set; }
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }
    [JsonPropertyName("tool_calls")]
    public List<OpenAiToolCall>? ToolCalls { get; set; }
}

internal sealed class OpenAiTool
{
    public string Type { get; set; } = "function";
    public OpenAiFunction Function { get; set; } = new();
}

internal sealed class OpenAiFunction
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object Parameters { get; set; } = new { type = "object", properties = new { } };
}

internal sealed class OpenAiChatResponse
{
    public List<OpenAiChoice> Choices { get; set; } = new();
}

internal sealed class OpenAiChoice
{
    public OpenAiMessage Message { get; set; } = new();
}

internal sealed class OpenAiToolCall
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "function";
    public OpenAiFunctionCall Function { get; set; } = new();
}

internal sealed class OpenAiFunctionCall
{
    public string Name { get; set; } = "";
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
