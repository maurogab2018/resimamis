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

    /// <summary>Intentos por llamada: el primero del día suele fallar por red fría o un 429/5xx pasajero.</summary>
    private const int MaxIntentos = 3;
    private static readonly TimeSpan TimeoutPorIntento = TimeSpan.FromSeconds(60);

    public async Task<OpenAiChatResponse> CompletarAsync(
        string apiKey,
        OpenAiChatRequest request,
        CancellationToken cancellationToken)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                return await CompletarUnaVezAsync(apiKey, request, cancellationToken);
            }
            catch (OpenAiTransitorioException ex) when (intento < MaxIntentos && !cancellationToken.IsCancellationRequested)
            {
                var espera = ex.Espera ?? TimeSpan.FromSeconds(intento * 2);
                if (espera > TimeSpan.FromSeconds(8)) espera = TimeSpan.FromSeconds(8);
                Console.WriteLine($"[asistente] OpenAI falló (intento {intento}/{MaxIntentos}): {ex.Message}. Reintento en {espera.TotalSeconds}s");
                await Task.Delay(espera, cancellationToken);
            }
        }
    }

    private async Task<OpenAiChatResponse> CompletarUnaVezAsync(
        string apiKey,
        OpenAiChatRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("OpenAI");
        // Relativo a la BaseAddress del cliente "OpenAI" (Asistente:BaseUrl, por defecto la API real):
        // así se puede probar contra un servidor simulado sin gastar tokens.
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        // Tope por intento: si uno se cuelga, se corta y se reintenta en vez de esperar los 180 s del cliente.
        using var porIntento = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        porIntento.CancelAfter(TimeoutPorIntento);
        try
        {
            using var response = await client.SendAsync(
                httpRequest,
                porIntento.Token);
            var body = await response.Content.ReadAsStringAsync(porIntento.Token);
            if (!response.IsSuccessStatusCode)
            {
                var detalle = ExtraerErrorOpenAi(body);
                var codigo = (int)response.StatusCode;
                var mensaje = $"Error al consultar OpenAI ({codigo}): {detalle}";
                // 408/409/429/5xx son pasajeros; 400/401/403/404 no se arreglan reintentando.
                if (codigo is 408 or 409 or 429 or >= 500)
                    throw new OpenAiTransitorioException(mensaje, response.Headers.RetryAfter?.Delta);
                throw new ApplicationException(mensaje);
            }

            OpenAiChatResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body, JsonOptions);
            }
            catch (JsonException)
            {
                throw new OpenAiTransitorioException("OpenAI devolvió una respuesta ilegible.");
            }
            if (parsed?.Choices == null || parsed.Choices.Count == 0 || parsed.Choices[0].Message == null)
                throw new OpenAiTransitorioException("OpenAI no devolvió una respuesta.");
            return parsed;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OpenAiTransitorioException("OpenAI tardó demasiado en responder.");
        }
        catch (HttpRequestException ex)
        {
            throw new OpenAiTransitorioException("No se pudo conectar con OpenAI: " + ex.Message);
        }
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

/// <summary>Falla de OpenAI que puede andar al reintentar (red, timeout, 429, 5xx, respuesta vacía).</summary>
internal sealed class OpenAiTransitorioException : ApplicationException
{
    public TimeSpan? Espera { get; }

    public OpenAiTransitorioException(string message, TimeSpan? espera = null) : base(message)
    {
        Espera = espera;
    }
}

internal sealed class OpenAiChatRequest
{
    public string Model { get; set; } = "";
    public List<OpenAiMessage> Messages { get; set; } = new();
    public List<OpenAiTool>? Tools { get; set; }
    /// <summary>"auto" o "required" (obliga a usar al menos una herramienta). Null = default de OpenAI.</summary>
    public string? ToolChoice { get; set; }
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
    public OpenAiUsage? Usage { get; set; }
}

/// <summary>Tokens que cobró OpenAI por la llamada.</summary>
internal sealed class OpenAiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
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
