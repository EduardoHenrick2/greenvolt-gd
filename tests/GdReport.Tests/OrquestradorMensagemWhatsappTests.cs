namespace GdReport.Tests;
public class OrquestradorMensagemWhatsappTests
{
    private sealed class ClienteFake(params Func<string>[] respostas) : IClienteLlm
    {
        private int _proxima;
        public List<string> Prompts { get; } = [];
        public Task<string> GerarTextoAsync(string prompt, CancellationToken cancellationToken = default)
        {
            Prompts.Add(prompt);
            var resposta = respostas[Math.Min(_proxima++, respostas.Length - 1)];
            return Task.FromResult(resposta());
        }
    }
    private static RelatorioApuracao RelatorioDoExemplo()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "Relatorio_GD_UFV_Exemplo_2026-09.xml");
        return ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(caminho));
    }
    private const string MensagemFiel = "*Resumo* da usina 3010000000: gerou 12.000 e transferiu 11.800. 3 alertas em 11 faturas.";
    private const string MensagemAlucinada = "*Resumo* da usina 3010000000: gerou 12.500 e transferiu 11.800.";
    private static string Fallback(RelatorioApuracao relatorio)
    {
        return GeradorMensagemWhatsapp.GerarFallbackDeterministico(relatorio);
    }
    [Fact]
    public async Task GerarAsync_SemCliente_DeveUsarFallbackConferido()
    {
        var relatorio = RelatorioDoExemplo();
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio);
        Assert.Equal(Fallback(relatorio), resultado.Mensagem);
        Assert.Equal("Fallback determinístico", resultado.Origem);
        Assert.Equal(0, resultado.Tentativas);
        Assert.True(resultado.Conferida);
    }
    [Fact]
    public async Task GerarAsync_LlmAcertaNaPrimeira_DeveRetornarMensagemDaLlm()
    {
        var cliente = new ClienteFake(() => MensagemFiel);
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(RelatorioDoExemplo(), cliente);
        Assert.Equal(MensagemFiel, resultado.Mensagem);
        Assert.Equal("LLM (tentativa 1/2)", resultado.Origem);
        Assert.Equal(1, resultado.Tentativas);
        Assert.True(resultado.Conferida);
        Assert.Single(cliente.Prompts);
    }
    [Fact]
    public async Task GerarAsync_LlmCorrigeNaSegunda_DeveEnviarPromptDeCorrecao()
    {
        var relatorio = RelatorioDoExemplo();
        var cliente = new ClienteFake(() => MensagemAlucinada, () => MensagemFiel);
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio, cliente);
        Assert.Equal(MensagemFiel, resultado.Mensagem);
        Assert.Equal("LLM (tentativa 2/2)", resultado.Origem);
        Assert.Equal(2, resultado.Tentativas);
        Assert.True(resultado.Conferida);
        Assert.Equal(2, cliente.Prompts.Count);
        Assert.Equal(GeradorMensagemWhatsapp.MontarPrompt(relatorio), cliente.Prompts[0]);
        Assert.Contains(MensagemAlucinada, cliente.Prompts[1]);
        Assert.Contains("12.500", cliente.Prompts[1]);
    }
    [Fact]
    public async Task GerarAsync_LlmAlucinaEmTodas_DeveCairNoFallback()
    {
        var relatorio = RelatorioDoExemplo();
        var cliente = new ClienteFake(() => MensagemAlucinada);
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio, cliente);
        Assert.Equal(Fallback(relatorio), resultado.Mensagem);
        Assert.Equal("Fallback determinístico", resultado.Origem);
        Assert.Equal(2, resultado.Tentativas);
        Assert.True(resultado.Conferida);
        Assert.Equal(2, cliente.Prompts.Count);
    }
    public static TheoryData<Exception> ErrosRecuperaveis => new()
    {
        new HttpRequestException("500"),
        new InvalidOperationException("sem conteúdo"),
        new TaskCanceledException("timeout")
    };
    [Theory]
    [MemberData(nameof(ErrosRecuperaveis))]
    public async Task GerarAsync_LlmFalha_DeveCairNoFallbackSemPropagar(Exception erro)
    {
        var relatorio = RelatorioDoExemplo();
        var cliente = new ClienteFake(() => throw erro);
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio, cliente);
        Assert.Equal(Fallback(relatorio), resultado.Mensagem);
        Assert.Equal("Fallback determinístico", resultado.Origem);
        Assert.Equal(1, resultado.Tentativas);
        Assert.True(resultado.Conferida);
    }
    [Fact]
    public async Task GerarAsync_FalhaNaSegundaTentativa_DeveCairNoFallback()
    {
        var relatorio = RelatorioDoExemplo();
        var cliente = new ClienteFake(() => MensagemAlucinada, () => throw new HttpRequestException("fora do ar"));
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio, cliente);
        Assert.Equal("Fallback determinístico", resultado.Origem);
        Assert.Equal(2, resultado.Tentativas);
        Assert.True(resultado.Conferida);
    }
    [Fact]
    public async Task GerarAsync_CancelamentoDoChamador_DevePropagar()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cliente = new ClienteFake(() => MensagemFiel);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => OrquestradorMensagemWhatsapp.GerarAsync(RelatorioDoExemplo(), cliente, cancellationToken: cts.Token));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GerarAsync_MaxTentativasInvalido_DeveLancarArgumentOutOfRange(int maxTentativas)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => OrquestradorMensagemWhatsapp.GerarAsync(RelatorioDoExemplo(), new ClienteFake(() => MensagemFiel), maxTentativas));
    }
}