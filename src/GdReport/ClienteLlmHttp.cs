using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace GdReport;
// Endpoint é a URL completa da rota de chat (ex.: http://localhost:11434/v1/chat/completions).
public record ConfiguracaoLlm(string Endpoint, string Modelo, string? ApiKey = null, TimeSpan? Timeout = null)
{
    private const string ModeloPadrao = "llama3.2";
    private static readonly TimeSpan TimeoutPadrao = TimeSpan.FromSeconds(15);
    // Retorna null quando GD_LLM_ENDPOINT não está definida (LLM não configurada).
    public static ConfiguracaoLlm? DeVariaveisAmbiente()
    {
        return DeVariaveisAmbiente(Environment.GetEnvironmentVariable);
    }
    public static ConfiguracaoLlm? DeVariaveisAmbiente(Func<string, string?> lerVariavel)
    {
        var endpoint = lerVariavel("GD_LLM_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }
        var modelo = lerVariavel("GD_LLM_MODEL");
        var apiKey = lerVariavel("GD_LLM_API_KEY");
        return new ConfiguracaoLlm(
            endpoint.Trim(),
            string.IsNullOrWhiteSpace(modelo) ? ModeloPadrao : modelo.Trim(),
            string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim(),
            LerTimeout(lerVariavel("GD_LLM_TIMEOUT_SEGUNDOS")));
    }
    private static TimeSpan LerTimeout(string? segundos)
    {
        var valido = int.TryParse(segundos, NumberStyles.None, CultureInfo.InvariantCulture, out var valor) && valor > 0;
        return valido ? TimeSpan.FromSeconds(valor) : TimeoutPadrao;
    }
}
public sealed class ClienteLlmHttp : IClienteLlm
{
    private readonly ConfiguracaoLlm _configuracao;
    private readonly HttpClient _http;
    public ClienteLlmHttp(ConfiguracaoLlm configuracao, HttpClient? http = null)
    {
        _configuracao = configuracao;
        _http = http ?? new HttpClient();
    }
    public async Task<string> GerarTextoAsync(string prompt, CancellationToken cancellationToken = default)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(_configuracao.Timeout ?? TimeSpan.FromSeconds(15));
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, _configuracao.Endpoint)
        {
            Content = JsonContent.Create(new
            {
                model = _configuracao.Modelo,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0
            })
        };
        if (!string.IsNullOrWhiteSpace(_configuracao.ApiKey))
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracao.ApiKey);
        }
        using var resposta = await _http.SendAsync(requisicao, limite.Token);
        if (!resposta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"A LLM respondeu com status HTTP {(int)resposta.StatusCode} ({resposta.StatusCode}).",
                null,
                resposta.StatusCode);
        }
        var json = await resposta.Content.ReadAsStringAsync(limite.Token);
        return ExtrairConteudo(json);
    }
    private static string ExtrairConteudo(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            if (documento.RootElement.ValueKind == JsonValueKind.Object
                && documento.RootElement.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0
                && choices[0].ValueKind == JsonValueKind.Object
                && choices[0].TryGetProperty("message", out var mensagem)
                && mensagem.ValueKind == JsonValueKind.Object
                && mensagem.TryGetProperty("content", out var conteudo)
                && conteudo.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(conteudo.GetString()))
            {
                return conteudo.GetString()!.Trim();
            }
        }
        catch (JsonException erro)
        {
            throw new InvalidOperationException("A resposta da LLM não é um JSON válido.", erro);
        }
        throw new InvalidOperationException("A resposta da LLM não contém 'choices[0].message.content' preenchido.");
    }
}