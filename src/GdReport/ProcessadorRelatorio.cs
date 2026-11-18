using System.Globalization;

namespace GdReport;

public static class ProcessadorRelatorio
{
    public static RelatorioApuracao Processar(IEnumerable<RegistroGd> registros)
    {
        var lista = registros.ToList();
        ValidarPeriodos(lista);

        var usinas = Agrupar(lista.Where(EhUsinaGeradora))
            .Select(g => ApurarUsina(g.Instalacao, g.PeriodoUso, g.Linhas))
            .ToList();

        var faturasUc = Agrupar(lista.Where(r => !EhUsinaGeradora(r)))
            .Select(g => ApurarFaturaUc(g.PeriodoUso, g.Linhas))
            .OrderBy(f => f.Uc, StringComparer.Ordinal)
            .ThenBy(f => ChavePeriodo(f.Fatura))
            .ToList();

        return new RelatorioApuracao(usinas, faturasUc);
    }

    private static bool EhUsinaGeradora(RegistroGd registro)
    {
        return registro.Modalidade.Contains("Geradora", StringComparison.OrdinalIgnoreCase)
            && !registro.Modalidade.Contains("Recebedora", StringComparison.OrdinalIgnoreCase);
    }

    // Agrupa por Instalacao + PeriodoUso e já devolve as linhas de cada grupo em ordem cronológica de Periodo.
    private static IEnumerable<(string Instalacao, string PeriodoUso, List<RegistroGd> Linhas)> Agrupar(
        IEnumerable<RegistroGd> registros)
    {
        return registros
            .GroupBy(r => (r.Instalacao, r.PeriodoUso))
            .OrderBy(g => g.Key.Instalacao, StringComparer.Ordinal)
            .ThenBy(g => ChavePeriodo(g.Key.PeriodoUso))
            .Select(g => (g.Key.Instalacao, g.Key.PeriodoUso, Linhas: g.OrderBy(l => ChavePeriodo(l.Periodo)).ToList()));
    }

    private static LinhaUsina ApurarUsina(string instalacao, string periodoUso, List<RegistroGd> linhas)
    {
        return new LinhaUsina(
            Instalacao: instalacao,
            PeriodoUso: periodoUso,
            Geracao: linhas.Sum(l => l.QtdGeracao),
            Transferencia: linhas.Sum(l => l.QtdTransferencia),
            // Valor da fatura inteira: repete entre linhas do grupo, por isso não é somado.
            CompensacaoPropria: linhas[0].QtdCompensacao,
            SaldoAnterior: linhas[0].QtdSaldoAnt,
            Saldo: linhas[^1].QtdSaldoAtual);
    }

    private static LinhaFaturaUc ApurarFaturaUc(string periodoUso, List<RegistroGd> linhas)
    {
        // Consumo, Compensado e Quota são valores da fatura inteira: repetem entre as linhas do grupo, então não são somados.
        var primeira = linhas[0];

        return new LinhaFaturaUc(
            Uc: primeira.NumeroUc,
            Fatura: periodoUso,
            Quota: primeira.Quota,
            Consumo: primeira.QtdConsumo,
            Recebido: linhas.Sum(l => l.QtdRecebimento),
            Compensado: primeira.QtdCompensacao,
            SaldoAnterior: primeira.QtdSaldoAnt,
            Saldo: linhas[^1].QtdSaldoAtual,
            GeracaoPropria: linhas.Sum(l => l.QtdGeracao));
    }

    private static void ValidarPeriodos(IEnumerable<RegistroGd> registros)
    {
        foreach (var registro in registros)
        {
            ChavePeriodo(registro.Periodo);
            ChavePeriodo(registro.PeriodoUso);
        }
    }

    // Aceita "yyyy-MM" e "MM/yyyy". Devolve (ano, mês) para ordenar cronologicamente.
    private static (int Ano, int Mes) ChavePeriodo(string periodo)
    {
        var partes = periodo.Split('-', '/');

        if (partes.Length == 2
            && int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var primeiro)
            && int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var segundo))
        {
            return partes[0].Length == 4 ? (primeiro, segundo) : (segundo, primeiro);
        }

        throw new FormatException($"Período inválido: '{periodo}'.");
    }
}
