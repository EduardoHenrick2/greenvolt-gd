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
}
