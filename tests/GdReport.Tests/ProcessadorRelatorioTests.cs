namespace GdReport.Tests;

public class ProcessadorRelatorioTests
{
    private static RegistroGd Registro(
        string instalacao,
        string modalidade,
        string periodo = "2026-09",
        string periodoUso = "2026-09",
        decimal geracao = 0m,
        decimal transferencia = 0m,
        decimal compensacao = 0m,
        decimal saldoAnt = 0m,
        decimal saldoAtual = 0m)
    {
        return new RegistroGd(
            Instalacao: instalacao,
            Modalidade: modalidade,
            Periodo: periodo,
            PeriodoUso: periodoUso,
            Quota: 0m,
            QtdConsumo: 0m,
            QtdRecebimento: 0m,
            QtdTransferencia: transferencia,
            QtdCompensacao: compensacao,
            QtdGeracao: geracao,
            QtdSaldoAnt: saldoAnt,
            QtdSaldoAtual: saldoAtual);
    }

    [Fact]
    public void Processar_DeveColocarApenasAGeradoraEmUsinas()
    {
        var registros = new[]
        {
            Registro("3001", "Geração compartilhada - Geradora", geracao: 100m),
            Registro("3002", "Geração compartilhada - Recebedora"),
            Registro("3003", "Geradora e Recebedora")
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        var usina = Assert.Single(relatorio.Usinas);
        Assert.Equal("3001", usina.Instalacao);
        Assert.DoesNotContain(relatorio.FaturasUc, f => f.Uc == "3001");
    }

    [Fact]
    public void Processar_DeveSomarGeracaoETransferenciaEManterCompensacaoNoMesmoGrupo()
    {
        var registros = new[]
        {
            Registro("3001", "Geradora", periodo: "2026-09", periodoUso: "2026-09",
                geracao: 50m, transferencia: 40m, compensacao: 30m, saldoAnt: 7m, saldoAtual: 5m),
            Registro("3001", "Geradora", periodo: "2026-08", periodoUso: "2026-09",
                geracao: 100m, transferencia: 80m, compensacao: 30m, saldoAnt: 10m, saldoAtual: 7m)
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        var usina = Assert.Single(relatorio.Usinas);
        Assert.Equal("3001", usina.Instalacao);
        Assert.Equal("2026-09", usina.PeriodoUso);
        Assert.Equal(150m, usina.Geracao);
        Assert.Equal(120m, usina.Transferencia);
        Assert.Equal(30m, usina.CompensacaoPropria);
        Assert.Equal(10m, usina.SaldoAnterior);
        Assert.Equal(5m, usina.Saldo);
    }

    [Fact]
    public void Processar_DeveSepararGruposPorInstalacaoEPeriodoUso()
    {
        var registros = new[]
        {
            Registro("3001", "Geradora", periodoUso: "2026-09", geracao: 20m),
            Registro("3001", "Geradora", periodoUso: "2026-08", geracao: 10m),
            Registro("3000", "Geradora", periodoUso: "2026-09", geracao: 5m)
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        Assert.Collection(relatorio.Usinas,
            u => Assert.Equal(("3000", "2026-09", 5m), (u.Instalacao, u.PeriodoUso, u.Geracao)),
            u => Assert.Equal(("3001", "2026-08", 10m), (u.Instalacao, u.PeriodoUso, u.Geracao)),
            u => Assert.Equal(("3001", "2026-09", 20m), (u.Instalacao, u.PeriodoUso, u.Geracao)));
    }

    [Fact]
    public void Processar_SemRegistros_DeveRetornarListasVazias()
    {
        var relatorio = ProcessadorRelatorio.Processar([]);

        Assert.Empty(relatorio.Usinas);
        Assert.Empty(relatorio.FaturasUc);
    }

    [Fact]
    public void Processar_ArquivoReal_UsinaDeveSatisfazerGeracaoIgualTransferenciaMaisCompensacao()
    {
        var registros = LeitorXml.LerArquivo(CaminhoArquivoReal());

        var relatorio = ProcessadorRelatorio.Processar(registros);

        Assert.Collection(relatorio.Usinas,
            u => Assert.Equal(("3010000000", "2026-08"), (u.Instalacao, u.PeriodoUso)),
            u => Assert.Equal(("3010000000", "2026-09"), (u.Instalacao, u.PeriodoUso)));
        Assert.All(relatorio.Usinas, u => Assert.Equal(u.Transferencia + u.CompensacaoPropria, u.Geracao));
        Assert.Equal(11500m, relatorio.Usinas[0].Geracao);
        Assert.Equal(12000m, relatorio.Usinas[1].Geracao);
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
