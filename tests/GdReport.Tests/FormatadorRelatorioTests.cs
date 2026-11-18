namespace GdReport.Tests;

public class FormatadorRelatorioTests
{
    [Theory]
    [InlineData(2250, "2.250")]
    [InlineData(1830, "1.830")]
    [InlineData(0, "0")]
    [InlineData(1.5, "1,5")]
    [InlineData(1000.755, "1.000,755")]
    [InlineData(-150.5, "-150,5")]
    public void Valor_DeveFormatarEmPtBrSemZerosDesnecessarios(double valor, string esperado)
    {
        Assert.Equal(esperado, FormatadorRelatorio.Valor((decimal)valor));
    }

    [Theory]
    [InlineData(20, "20%")]
    [InlineData(12.5, "12,5%")]
    public void Percentual_DeveAcrescentarOSinalDePorcentagem(double quota, string esperado)
    {
        Assert.Equal(esperado, FormatadorRelatorio.Percentual((decimal)quota));
    }

    [Fact]
    public void TabelaFaturasUc_DeveFormatarLinhasComESemAlerta()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 2250m, 2260m, 2250m, 140m, 150m, 0m),
            new("7000110033", "2026-09", 15m, 1600m, 1695m, 1600m, 40m, 60m, 0m,
                "calculado 135, arquivo 60 (diferença 75)")
        ];

        var tabela = FormatadorRelatorio.TabelaFaturasUc(faturas);

        Assert.Equal(
            string.Join("\n",
                "| UC | Fatura | Quota | Consumo | Recebido | Compensado | Saldo | Geração própria | Alerta |",
                "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
                "| 7000110011 | 2026-08 | 20% | 2.250 | 2.260 | 2.250 | 150 | 0 |  |",
                "| 7000110033 | 2026-09 | 15% | 1.600 | 1.695 | 1.600 | 60 | 0 | calculado 135, arquivo 60 (diferença 75) |"),
            tabela);
    }

    [Fact]
    public void TabelaUsinas_DeveFormatarAUsinaApurada()
    {
        LinhaUsina[] usinas =
        [
            new("3010000000", "2026-08", 11500m, 11300m, 200m, 0m, 0m)
        ];

        var tabela = FormatadorRelatorio.TabelaUsinas(usinas);

        Assert.Equal(
            string.Join("\n",
                "Usina geradora",
                "| Instalação | Período de uso | Geração | Transferência | Compensação própria | Saldo anterior | Saldo |",
                "| --- | --- | --- | --- | --- | --- | --- |",
                "| 3010000000 | 2026-08 | 11.500 | 11.300 | 200 | 0 | 0 |"),
            tabela);
    }

    [Fact]
    public void TabelaUsinas_SemUsinas_DeveInformarQueNenhumaFoiApurada()
    {
        Assert.Equal("Usina geradora: nenhuma apurada.", FormatadorRelatorio.TabelaUsinas([]));
    }

    [Fact]
    public void Alertas_DeveListarApenasAsFaturasComAlerta()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 2250m, 2260m, 2250m, 140m, 150m, 0m),
            new("7000110033", "2026-09", 15m, 1600m, 1695m, 1600m, 40m, 60m, 0m,
                "calculado 135, arquivo 60 (diferença 75)")
        ];

        Assert.Equal(
            string.Join("\n",
                "Alertas",
                "- UC 7000110033, fatura 2026-09: calculado 135, arquivo 60 (diferença 75)"),
            FormatadorRelatorio.Alertas(faturas));
    }

    [Fact]
    public void Alertas_SemInconsistencias_DeveInformarQueNaoHaAlertas()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 2250m, 2260m, 2250m, 140m, 150m, 0m)
        ];

        Assert.Equal(
            string.Join("\n", "Alertas", "Nenhum alerta de inconsistência."),
            FormatadorRelatorio.Alertas(faturas));
    }

    private const string CabecalhoCsv = "UC,fatura,quota,consumo,recebido,compensado,saldo,geração própria,alerta";

    [Fact]
    public void Csv_SemFaturas_DeveTrazerSoOCabecalhoExato()
    {
        Assert.Equal(CabecalhoCsv, FormatadorRelatorio.Csv([]));
    }

    [Fact]
    public void Csv_LinhaQueFecha_DeveDeixarACelulaAlertaVazia()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 2250m, 2260m, 2250m, 140m, 150m, 0m)
        ];

        Assert.Equal(
            string.Join("\n",
                CabecalhoCsv,
                "7000110011,2026-08,20%,2.250,2.260,2.250,150,0,"),
            FormatadorRelatorio.Csv(faturas));
    }

    [Fact]
    public void Csv_LinhaComAlerta_DeveEntreAspasAOTextoQueContemVirgula()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110033", "2026-09", 15m, 1600m, 1695m, 1600m, 40m, 60m, 0m,
                "calculado 135, arquivo 60 (diferença 75)")
        ];

        Assert.Equal(
            string.Join("\n",
                CabecalhoCsv,
                "7000110033,2026-09,15%,1.600,1.695,1.600,60,0,\"calculado 135, arquivo 60 (diferença 75)\""),
            FormatadorRelatorio.Csv(faturas));
    }

    [Fact]
    public void Csv_ValorComVirgulaDecimal_DeveSerEntreAspas()
    {
        // Os valores seguem o pt-BR da tabela, então "1.000,755" também contém vírgula.
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 1000.755m, 0m, 0m, 0m, 0m, 0m)
        ];

        Assert.Equal(
            string.Join("\n",
                CabecalhoCsv,
                "7000110011,2026-08,20%,\"1.000,755\",0,0,0,0,"),
            FormatadorRelatorio.Csv(faturas));
    }

    [Fact]
    public void Csv_TextoComAspasEPontoEVirgula_DeveEscaparAsAspasDuplicando()
    {
        LinhaFaturaUc[] faturas =
        [
            new("7000110011", "2026-08", 20m, 0m, 0m, 0m, 0m, 0m, 0m, "diz \"oi\"; ok")
        ];

        var linha = FormatadorRelatorio.Csv(faturas).Split('\n')[1];

        Assert.EndsWith(",\"diz \"\"oi\"\"; ok\"", linha);
    }

    [Fact]
    public void Relatorio_DeveJuntarUsinaFaturasEAlertasEmUmTexto()
    {
        var relatorio = new RelatorioApuracao(
            [new LinhaUsina("3010000000", "2026-08", 11500m, 11300m, 200m, 0m, 0m)],
            [new LinhaFaturaUc("7000110033", "2026-09", 15m, 1600m, 1695m, 1600m, 40m, 60m, 0m,
                "calculado 135, arquivo 60 (diferença 75)")]);

        var texto = FormatadorRelatorio.Relatorio(relatorio);

        Assert.Equal(
            FormatadorRelatorio.TabelaUsinas(relatorio.Usinas) + "\n\n"
            + FormatadorRelatorio.TabelaFaturasUc(relatorio.FaturasUc) + "\n\n"
            + FormatadorRelatorio.Alertas(relatorio.FaturasUc),
            texto);
    }

    [Fact]
    public void Csv_ArquivoReal_DeveTerOnzeLinhasDeFaturaETresAlertas()
    {
        var faturas = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal())).FaturasUc;

        var linhas = FormatadorRelatorio.Csv(faturas).Split('\n');

        Assert.Equal(CabecalhoCsv, linhas[0]);
        Assert.Equal(12, linhas.Length);
        Assert.Equal(3, linhas.Count(l => l.EndsWith('"')));
    }

    private static string CaminhoArquivoReal()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "GreenVoltGd.slnx")))
        {
            pasta = pasta.Parent;
        }

        Assert.NotNull(pasta);
        return Path.Combine(pasta.FullName, "data", "Relatorio_GD_UFV_Exemplo_2026-09.xml");
    }
}
