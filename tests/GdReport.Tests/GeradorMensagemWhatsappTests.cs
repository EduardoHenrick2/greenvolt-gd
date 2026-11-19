namespace GdReport.Tests;
public class GeradorMensagemWhatsappTests
{
    private static RelatorioApuracao RelatorioDoExemplo()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "Relatorio_GD_UFV_Exemplo_2026-09.xml");
        return ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(caminho));
    }
    private static RelatorioApuracao RelatorioSemAlertas()
    {
        return new RelatorioApuracao(
            [new LinhaUsina("3010000000", "2026-09", 12000m, 11800m, 200m, 0m, 0m)],
            [new LinhaFaturaUc("7000110011", "2026-09", 20m, 2400m, 2360m, 2300m, 150m, 210m, 0m)]);
    }
    [Fact]
    public void MontarPrompt_DeveIncluirDadosOficiaisDoRelatorio()
    {
        var prompt = GeradorMensagemWhatsapp.MontarPrompt(RelatorioDoExemplo());
        Assert.Contains("3010000000", prompt);
        Assert.Contains("12.000", prompt);
        Assert.Contains("11.800", prompt);
        Assert.Contains("11", prompt);
        Assert.Contains("calculado 135, arquivo 60 (diferença 75)", prompt);
        Assert.Contains("calculado 1.830, arquivo 155 (diferença 1.675)", prompt);
        Assert.Contains("calculado 30, arquivo 530 (diferença 500)", prompt);
        Assert.Contains("7000110033", prompt);
    }
    [Fact]
    public void MontarPrompt_DeveInstruirANaoInventarNemRecalcularNumeros()
    {
        var prompt = GeradorMensagemWhatsapp.MontarPrompt(RelatorioDoExemplo());
        Assert.Contains("WhatsApp", prompt);
        Assert.Contains("*negrito*", prompt);
        Assert.Contains("não recalcule", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("não invente", prompt, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void MontarPromptCorrecao_DeveIncluirRascunhoEDivergencias()
    {
        var prompt = GeradorMensagemWhatsapp.MontarPromptCorrecao(
            RelatorioDoExemplo(),
            "Gerou 12.500 kWh e 1.850 de diferença",
            ["Número divergente: 12.500", "Número divergente: 1.850"]);
        Assert.Contains("Gerou 12.500 kWh e 1.850 de diferença", prompt);
        Assert.Contains("Número divergente: 12.500", prompt);
        Assert.Contains("Número divergente: 1.850", prompt);
        Assert.Contains("12.000", prompt);
    }
    [Fact]
    public void GerarFallbackDeterministico_ComAlertas_DeveListarOsTresAlertasEPassarNaConferencia()
    {
        var relatorio = RelatorioDoExemplo();
        var mensagem = GeradorMensagemWhatsapp.GerarFallbackDeterministico(relatorio);
        Assert.Contains("calculado 135, arquivo 60 (diferença 75)", mensagem);
        Assert.Contains("calculado 1.830, arquivo 155 (diferença 1.675)", mensagem);
        Assert.Contains("calculado 30, arquivo 530 (diferença 500)", mensagem);
        var resultado = ConferidorMensagem.Conferir(mensagem, relatorio);
        Assert.True(resultado.Valido, string.Join("; ", resultado.Divergencias));
        Assert.Empty(resultado.Divergencias);
    }
    [Fact]
    public void GerarFallbackDeterministico_SemAlertas_DeveAvisarEPassarNaConferencia()
    {
        var relatorio = RelatorioSemAlertas();
        var mensagem = GeradorMensagemWhatsapp.GerarFallbackDeterministico(relatorio);
        Assert.Contains("Nenhum alerta", mensagem);
        var resultado = ConferidorMensagem.Conferir(mensagem, relatorio);
        Assert.True(resultado.Valido, string.Join("; ", resultado.Divergencias));
    }
}