namespace GdReport.Tests;
public class ConferidorMensagemTests
{
    private static RelatorioApuracao RelatorioDoExemplo()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "Relatorio_GD_UFV_Exemplo_2026-09.xml");
        return ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(caminho));
    }
    private const string MensagemFiel =
        "Competência 2026-09 (09/2026). Usina 3010000000 gerou 12.000 kWh e transferiu 11.800. " +
        "A UC 7000110033 tem quota de 15% e alerta com diferença de 1.675 (calculado 1.830, arquivo 155). " +
        "Foram 11 faturas: 8 sem alerta e 3 com alerta, para 2 usinas.";
    [Fact]
    public void Conferir_MensagemFielAoXml_DeveSerValida()
    {
        var resultado = ConferidorMensagem.Conferir(MensagemFiel, RelatorioDoExemplo());
        Assert.True(resultado.Valido);
        Assert.Empty(resultado.Divergencias);
    }
    [Fact]
    public void Conferir_NumeroTrocado_DeveListarODivergente()
    {
        var mensagem = MensagemFiel.Replace("1.830", "1.850");
        var resultado = ConferidorMensagem.Conferir(mensagem, RelatorioDoExemplo());
        Assert.False(resultado.Valido);
        var divergencia = Assert.Single(resultado.Divergencias);
        Assert.Contains("1.850", divergencia);
    }
    [Fact]
    public void Conferir_DecimalPtBrPresenteNoRelatorio_DeveSerValido()
    {
        var relatorio = new RelatorioApuracao(
            [new LinhaUsina("3010000000", "2026-09", 0.755m, 0m, 0m, 0m, 0m)],
            []);
        var resultado = ConferidorMensagem.Conferir("A usina 3010000000 gerou 0,755 em 2026-09.", relatorio);
        Assert.True(resultado.Valido);
    }
    [Fact]
    public void Conferir_DecimalPtBrAusenteDoRelatorio_DeveSerDivergente()
    {
        var relatorio = new RelatorioApuracao(
            [new LinhaUsina("3010000000", "2026-09", 0.755m, 0m, 0m, 0m, 0m)],
            []);
        var resultado = ConferidorMensagem.Conferir("A usina 3010000000 gerou 0,775 em 2026-09.", relatorio);
        Assert.False(resultado.Valido);
        Assert.Contains("0,775", Assert.Single(resultado.Divergencias));
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void Conferir_MensagemVaziaOuEmBranco_DeveSerInvalida(string mensagem)
    {
        var resultado = ConferidorMensagem.Conferir(mensagem, RelatorioDoExemplo());
        Assert.False(resultado.Valido);
        Assert.Contains("vazia", Assert.Single(resultado.Divergencias));
    }
}