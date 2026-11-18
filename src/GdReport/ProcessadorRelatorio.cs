namespace GdReport;

public static class ProcessadorRelatorio
{
    public static RelatorioApuracao Processar(IEnumerable<RegistroGd> registros)
    {
        var usinas = registros
            .Where(EhUsinaGeradora)
            .GroupBy(r => (r.Instalacao, r.PeriodoUso))
            .OrderBy(g => g.Key.Instalacao, StringComparer.Ordinal)
            .ThenBy(g => g.Key.PeriodoUso, StringComparer.Ordinal)
            .Select(g => ApurarUsina(g.Key.Instalacao, g.Key.PeriodoUso, g))
            .ToList();

        return new RelatorioApuracao(usinas, []);
    }

    private static bool EhUsinaGeradora(RegistroGd registro)
    {
        return registro.Modalidade.Contains("Geradora", StringComparison.OrdinalIgnoreCase)
            && !registro.Modalidade.Contains("Recebedora", StringComparison.OrdinalIgnoreCase);
    }

    private static LinhaUsina ApurarUsina(string instalacao, string periodoUso, IEnumerable<RegistroGd> linhas)
    {
        var porPeriodo = linhas.OrderBy(l => l.Periodo, StringComparer.Ordinal).ToList();

        return new LinhaUsina(
            Instalacao: instalacao,
            PeriodoUso: periodoUso,
            Geracao: porPeriodo.Sum(l => l.QtdGeracao),
            Transferencia: porPeriodo.Sum(l => l.QtdTransferencia),
            // Valor da fatura inteira: repete entre linhas do grupo, por isso não é somado.
            CompensacaoPropria: porPeriodo[0].QtdCompensacao,
            SaldoAnterior: porPeriodo[0].QtdSaldoAnt,
            Saldo: porPeriodo[^1].QtdSaldoAtual);
    }
}
