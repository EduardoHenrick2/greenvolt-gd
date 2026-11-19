using System.Net;
using System.Text;
using System.Text.Json;
namespace GdReport.Tests;
public class ClienteLlmHttpTests
{
    private sealed class HandlerFake(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> resposta)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Requisicao { get; private set; }
        public string? Corpo { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requisicao = request;
            Corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return await resposta(request, cancellationToken);
        }
    }
    private static HandlerFake Responde(HttpStatusCode status, string json)
    {
        return new HandlerFake((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));
    }
    private static ClienteLlmHttp Cliente(HandlerFake handler, string? apiKey = null, TimeSpan? timeout = null)
    {
        var config = new ConfiguracaoLlm("http://llm.local/v1/chat/completions", "modelo-x", apiKey, timeout);
        return new ClienteLlmHttp(config, new HttpClient(handler));
    }
    private const string RespostaValida = """{"choices":[{"message":{"role":"assistant","content":"  Olá, mundo  "}}]}""";
    [Fact]
    public async Task GerarTextoAsync_RespostaValida_DeveEnviarPayloadERetornarConteudoSemEspacos()
    {
        var handler = Responde(HttpStatusCode.OK, RespostaValida);
        var texto = await Cliente(handler).GerarTextoAsync("meu prompt");
        Assert.Equal("Olá, mundo", texto);
        Assert.Equal(HttpMethod.Post, handler.Requisicao!.Method);
        Assert.Equal("http://llm.local/v1/chat/completions", handler.Requisicao.RequestUri!.ToString());
        using var corpo = JsonDocument.Parse(handler.Corpo!);
        var raiz = corpo.RootElement;
        Assert.Equal("modelo-x", raiz.GetProperty("model").GetString());
        Assert.Equal(0, raiz.GetProperty("temperature").GetDecimal());
        var mensagem = Assert.Single(raiz.GetProperty("messages").EnumerateArray());
        Assert.Equal("user", mensagem.GetProperty("role").GetString());
        Assert.Equal("meu prompt", mensagem.GetProperty("content").GetString());
    }
    [Fact]
    public async Task GerarTextoAsync_ComApiKey_DeveEnviarAuthorizationBearer()
    {
        var handler = Responde(HttpStatusCode.OK, RespostaValida);
        await Cliente(handler, apiKey: "segredo").GerarTextoAsync("p");
        Assert.Equal("Bearer", handler.Requisicao!.Headers.Authorization?.Scheme);
        Assert.Equal("segredo", handler.Requisicao.Headers.Authorization?.Parameter);
    }
    [Fact]
    public async Task GerarTextoAsync_SemApiKey_NaoDeveEnviarAuthorization()
    {
        var handler = Responde(HttpStatusCode.OK, RespostaValida);
        await Cliente(handler).GerarTextoAsync("p");
        Assert.Null(handler.Requisicao!.Headers.Authorization);
    }
    [Fact]
    public async Task GerarTextoAsync_Status500_DeveLancarHttpRequestException()
    {
        var handler = Responde(HttpStatusCode.InternalServerError, "erro");
        var erro = await Assert.ThrowsAsync<HttpRequestException>(() => Cliente(handler).GerarTextoAsync("p"));
        Assert.Contains("500", erro.Message);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"choices":[]}""")]
    [InlineData("""{"choices":[{}]}""")]
    [InlineData("""{"choices":[{"message":{}}]}""")]
    [InlineData("""{"choices":[{"message":{"content":null}}]}""")]
    [InlineData("""{"choices":[{"message":{"content":"   "}}]}""")]
    [InlineData("isto não é json")]
    public async Task GerarTextoAsync_RespostaSemConteudoOuMalformada_DeveLancarInvalidOperationException(string json)
    {
        var handler = Responde(HttpStatusCode.OK, json);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Cliente(handler).GerarTextoAsync("p"));
    }
    [Fact]
    public async Task GerarTextoAsync_TimeoutExcedido_DeveLancarOperationCanceledException()
    {
        var handler = new HandlerFake(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var cliente = Cliente(handler, timeout: TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cliente.GerarTextoAsync("p"));
    }
    [Fact]
    public async Task GerarTextoAsync_CancelamentoDoChamador_DeveLancarOperationCanceledException()
    {
        var handler = Responde(HttpStatusCode.OK, RespostaValida);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Cliente(handler).GerarTextoAsync("p", cts.Token));
    }
    private static Func<string, string?> Ambiente(Dictionary<string, string> valores)
    {
        return nome => valores.GetValueOrDefault(nome);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeVariaveisAmbiente_SemEndpoint_DeveRetornarNull(string? endpoint)
    {
        var valores = new Dictionary<string, string> { ["GD_LLM_MODEL"] = "x" };
        if (endpoint is not null)
        {
            valores["GD_LLM_ENDPOINT"] = endpoint;
        }
        Assert.Null(ConfiguracaoLlm.DeVariaveisAmbiente(Ambiente(valores)));
    }
    [Fact]
    public void DeVariaveisAmbiente_SoComEndpoint_DeveAplicarPadroes()
    {
        var config = ConfiguracaoLlm.DeVariaveisAmbiente(Ambiente(new() { ["GD_LLM_ENDPOINT"] = "http://x/v1/chat/completions" }));
        Assert.NotNull(config);
        Assert.Equal("http://x/v1/chat/completions", config.Endpoint);
        Assert.Equal("llama3.2", config.Modelo);
        Assert.Null(config.ApiKey);
        Assert.Equal(TimeSpan.FromSeconds(15), config.Timeout);
    }
    [Fact]
    public void DeVariaveisAmbiente_ComValoresCustomizados_DeveLerTodos()
    {
        var config = ConfiguracaoLlm.DeVariaveisAmbiente(Ambiente(new()
        {
            ["GD_LLM_ENDPOINT"] = "http://x",
            ["GD_LLM_MODEL"] = "gpt-4o-mini",
            ["GD_LLM_API_KEY"] = "abc",
            ["GD_LLM_TIMEOUT_SEGUNDOS"] = "30"
        }));
        Assert.NotNull(config);
        Assert.Equal("gpt-4o-mini", config.Modelo);
        Assert.Equal("abc", config.ApiKey);
        Assert.Equal(TimeSpan.FromSeconds(30), config.Timeout);
    }
    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void DeVariaveisAmbiente_TimeoutInvalido_DeveUsarPadrao(string timeout)
    {
        var config = ConfiguracaoLlm.DeVariaveisAmbiente(Ambiente(new()
        {
            ["GD_LLM_ENDPOINT"] = "http://x",
            ["GD_LLM_TIMEOUT_SEGUNDOS"] = timeout
        }));
        Assert.Equal(TimeSpan.FromSeconds(15), config!.Timeout);
    }
}