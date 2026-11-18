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
        decimal saldoAtual = 0m,
        string numeroUc = "",
        decimal quota = 0m,
        decimal consumo = 0m,
        decimal recebimento = 0m)
    {
        return new RegistroGd(
            Instalacao: instalacao,
            Modalidade: modalidade,
            Periodo: periodo,
            PeriodoUso: periodoUso,
            Quota: quota,
            QtdConsumo: consumo,
            QtdRecebimento: recebimento,
            QtdTransferencia: transferencia,
            QtdCompensacao: compensacao,
            QtdGeracao: geracao,
            QtdSaldoAnt: saldoAnt,
            QtdSaldoAtual: saldoAtual,
            NumeroUc: numeroUc);
    }

    private const string Recebedora = "Geração compartilhada - Recebedora";

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

    [Fact]
    public void Processar_DeveColocarEmFaturasUcTodaLinhaQueNaoEExclusivamenteUsina()
    {
        var registros = new[]
        {
            Registro("3001", "Geradora", numeroUc: "7001"),
            Registro("3002", Recebedora, numeroUc: "7002"),
            Registro("3003", "Geradora e Recebedora", numeroUc: "7003")
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        Assert.Equal(["7002", "7003"], relatorio.FaturasUc.Select(f => f.Uc));
    }

    [Fact]
    public void Processar_UcDeveSomarRecebidoEGeracaoPropriaENaoSomarConsumoCompensadoEQuota()
    {
        var registros = new[]
        {
            Registro("3002", Recebedora, periodo: "2026-08", periodoUso: "2026-09", numeroUc: "7002",
                quota: 35m, consumo: 8000m, compensacao: 7900m, recebimento: 3955m, geracao: 10m),
            // saldoAtual fecha a equação: 0 + 8085 + 30 - 7900 = 215.
            Registro("3002", Recebedora, periodo: "2026-09", periodoUso: "2026-09", numeroUc: "7002",
                quota: 35m, consumo: 8000m, compensacao: 7900m, recebimento: 4130m, geracao: 20m, saldoAtual: 215m)
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        var fatura = Assert.Single(relatorio.FaturasUc);
        Assert.Equal("7002", fatura.Uc);
        Assert.Equal("2026-09", fatura.Fatura);
        Assert.Equal(35m, fatura.Quota);
        Assert.Equal(8000m, fatura.Consumo);
        Assert.Equal(7900m, fatura.Compensado);
        Assert.Equal(8085m, fatura.Recebido);
        Assert.Equal(30m, fatura.GeracaoPropria);
        Assert.Null(fatura.Alerta);
    }

    [Fact]
    public void Processar_UcDeveCalcularSaldosPeloPeriodoMesmoComLinhasForaDeOrdem()
    {
        // O saldo anterior vem da linha de menor Periodo e o saldo final da de maior Periodo,
        // independentemente da ordem em que as linhas aparecem.
        var registros = new[]
        {
            Registro("3002", Recebedora, periodo: "2026-09", periodoUso: "2026-09", numeroUc: "7002",
                saldoAnt: 300m, saldoAtual: 485m),
            Registro("3002", Recebedora, periodo: "2026-08", periodoUso: "2026-09", numeroUc: "7002",
                saldoAnt: 50m, saldoAtual: 300m)
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        var fatura = Assert.Single(relatorio.FaturasUc);
        Assert.Equal(50m, fatura.SaldoAnterior);
        Assert.Equal(485m, fatura.Saldo);
    }

    [Fact]
    public void Processar_UcDeveOrdenarPeriodosCronologicamenteNosDoisFormatos()
    {
        var registros = new[]
        {
            Registro("3002", Recebedora, periodo: "01/2026", periodoUso: "01/2026", numeroUc: "7002", saldoAnt: 2m, saldoAtual: 3m),
            Registro("3002", Recebedora, periodo: "12/2025", periodoUso: "01/2026", numeroUc: "7002", saldoAnt: 1m, saldoAtual: 2m),
            Registro("3002", Recebedora, periodo: "2025-12", periodoUso: "2025-12", numeroUc: "7002"),
            Registro("3002", Recebedora, periodo: "2026-02", periodoUso: "2026-02", numeroUc: "7002")
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        Assert.Equal(["2025-12", "01/2026", "2026-02"], relatorio.FaturasUc.Select(f => f.Fatura));
        var janeiro = relatorio.FaturasUc.Single(f => f.Fatura == "01/2026");
        Assert.Equal(1m, janeiro.SaldoAnterior);
        Assert.Equal(3m, janeiro.Saldo);
    }

    [Fact]
    public void Processar_UcDeveOrdenarPorUcEDepoisPorFatura()
    {
        var registros = new[]
        {
            Registro("3002", Recebedora, periodoUso: "2026-09", numeroUc: "7002"),
            Registro("3001", Recebedora, periodoUso: "2026-09", numeroUc: "7001"),
            Registro("3001", Recebedora, periodoUso: "2026-08", numeroUc: "7001")
        };

        var relatorio = ProcessadorRelatorio.Processar(registros);

        Assert.Equal(
            [("7001", "2026-08"), ("7001", "2026-09"), ("7002", "2026-09")],
            relatorio.FaturasUc.Select(f => (f.Uc, f.Fatura)));
    }

    [Fact]
    public void Processar_PeriodoInvalido_DeveLancarFormatException()
    {
        var registros = new[] { Registro("3002", Recebedora, periodo: "agosto") };

        Assert.Throws<FormatException>(() => ProcessadorRelatorio.Processar(registros));
    }

    [Fact]
    public void Processar_ArquivoReal_DeveGerarOnzeFaturasSemALinhaDaUsina()
    {
        var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal()));

        Assert.Equal(11, relatorio.FaturasUc.Count);
        Assert.DoesNotContain(relatorio.FaturasUc, f => f.Uc == "7000100000");
    }

    [Fact]
    public void Processar_ArquivoReal_Uc7000110011Fatura2026_08()
    {
        var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal()));

        var fatura = relatorio.FaturasUc.Single(f => f.Uc == "7000110011" && f.Fatura == "2026-08");

        Assert.Equal(20m, fatura.Quota);
        Assert.Equal(2250m, fatura.Consumo);
        Assert.Equal(2260m, fatura.Recebido);
        Assert.Equal(2250m, fatura.Compensado);
        Assert.Equal(140m, fatura.SaldoAnterior);
        Assert.Equal(150m, fatura.Saldo);
        Assert.Equal(0m, fatura.GeracaoPropria);
        Assert.Null(fatura.Alerta);
    }

    [Fact]
    public void Processar_ArquivoReal_Uc7000110022SomaDuasLinhasDaMesmaFatura()
    {
        var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal()));

        var fatura = relatorio.FaturasUc.Single(f => f.Uc == "7000110022");

        Assert.Equal("2026-09", fatura.Fatura);
        Assert.Equal(8000m, fatura.Consumo);
        Assert.Equal(7900m, fatura.Compensado);
        Assert.Equal(8085m, fatura.Recebido);
        Assert.Equal(300m, fatura.SaldoAnterior);
        Assert.Equal(485m, fatura.Saldo);
    }

    [Fact]
    public void Processar_ArquivoReal_Uc7000110055DeveApurarGeracaoPropria()
    {
        var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal()));

        var faturas = relatorio.FaturasUc.Where(f => f.Uc == "7000110055").ToList();

        Assert.Equal(["2026-08", "2026-09"], faturas.Select(f => f.Fatura));
        Assert.Equal([280m, 300m], faturas.Select(f => f.GeracaoPropria));
    }

    [Fact]
    public void Processar_UcComSaldoQueFecha_NaoDeveGerarAlerta()
    {
        // 140 + 2260 + 0 - 2250 = 150
        var registros = new[]
        {
            Registro("3002", Recebedora, numeroUc: "7002",
                saldoAnt: 140m, recebimento: 2260m, compensacao: 2250m, saldoAtual: 150m)
        };

        var fatura = Assert.Single(ProcessadorRelatorio.Processar(registros).FaturasUc);

        Assert.Equal(150m, fatura.Saldo);
        Assert.Null(fatura.Alerta);
    }

    [Fact]
    public void Processar_UcComGeracaoPropriaQueFecha_NaoDeveGerarAlerta()
    {
        // 0 + 1356 + 280 - 1636 = 0
        var registros = new[]
        {
            Registro("3002", Recebedora, numeroUc: "7002",
                recebimento: 1356m, geracao: 280m, compensacao: 1636m, saldoAtual: 0m)
        };

        var fatura = Assert.Single(ProcessadorRelatorio.Processar(registros).FaturasUc);

        Assert.Null(fatura.Alerta);
    }

    [Fact]
    public void Processar_UcComSaldoDivergente_DevePreservarSaldoDoArquivoEGerarAlerta()
    {
        // 60 + 1770 + 0 - 0 = 1830, mas o arquivo informa 155.
        var registros = new[]
        {
            Registro("3002", Recebedora, numeroUc: "7002",
                saldoAnt: 60m, recebimento: 1770m, compensacao: 0m, saldoAtual: 155m)
        };

        var fatura = Assert.Single(ProcessadorRelatorio.Processar(registros).FaturasUc);

        Assert.Equal(155m, fatura.Saldo);
        Assert.Equal("calculado 1.830, arquivo 155 (diferença 1.675)", fatura.Alerta);
    }

    [Fact]
    public void Processar_UcComSaldoDivergenteNaoInteiro_DeveMostrarAsCasasDecimaisSemArredondar()
    {
        // 1000,5 + 0,255 - 0 = 1000,755, mas o arquivo informa 1000.
        var registros = new[]
        {
            Registro("3002", Recebedora, numeroUc: "7002",
                saldoAnt: 1000.5m, recebimento: 0.255m, saldoAtual: 1000m)
        };

        var fatura = Assert.Single(ProcessadorRelatorio.Processar(registros).FaturasUc);

        Assert.Equal("calculado 1.000,755, arquivo 1.000 (diferença 0,755)", fatura.Alerta);
    }

    [Fact]
    public void Processar_ArquivoReal_DeveGerarOitoFaturasSemAlertaETresComAlerta()
    {
        var faturas = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(CaminhoArquivoReal())).FaturasUc;

        Assert.Equal(11, faturas.Count);
        Assert.Equal(8, faturas.Count(f => f.Alerta is null));
        Assert.Equal(
            [
                ("7000110033", "2026-09", 60m, "calculado 135, arquivo 60 (diferença 75)"),
                ("7000110033", "2026-10", 155m, "calculado 1.830, arquivo 155 (diferença 1.675)"),
                ("7000110044", "2026-09", 530m, "calculado 30, arquivo 530 (diferença 500)")
            ],
            faturas.Where(f => f.Alerta is not null).Select(f => (f.Uc, f.Fatura, f.Saldo, f.Alerta!)));
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
