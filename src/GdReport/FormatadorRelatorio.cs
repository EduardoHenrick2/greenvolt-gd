using System.Globalization;

namespace GdReport;

// Só apresentação: transforma os modelos apurados em texto Markdown. Não calcula nada.
public static class FormatadorRelatorio
{
    // Separadores fixos de pt-BR, sem depender da cultura do sistema.
    private static readonly NumberFormatInfo FormatoPtBr = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    // Zeros à direita são omitidos e nenhuma casa é arredondada.
    public static string Valor(decimal valor)
    {
        return valor.ToString("#,##0.############################", FormatoPtBr);
    }

    public static string Percentual(decimal quota)
    {
        return $"{Valor(quota)}%";
    }

    public static string TabelaUsinas(IReadOnlyList<LinhaUsina> usinas)
    {
        if (usinas.Count == 0)
        {
            return "Usina geradora: nenhuma apurada.";
        }

        var linhas = usinas.Select(u => Linha(
            u.Instalacao,
            u.PeriodoUso,
            Valor(u.Geracao),
            Valor(u.Transferencia),
            Valor(u.CompensacaoPropria),
            Valor(u.SaldoAnterior),
            Valor(u.Saldo)));

        return string.Join("\n", new[]
        {
            "Usina geradora",
            Linha("Instalação", "Período de uso", "Geração", "Transferência", "Compensação própria", "Saldo anterior", "Saldo"),
            Linha("---", "---", "---", "---", "---", "---", "---"),
        }.Concat(linhas));
    }

    public static string TabelaFaturasUc(IReadOnlyList<LinhaFaturaUc> faturas)
    {
        var linhas = faturas.Select(f => Linha(
            f.Uc,
            f.Fatura,
            Percentual(f.Quota),
            Valor(f.Consumo),
            Valor(f.Recebido),
            Valor(f.Compensado),
            Valor(f.Saldo),
            Valor(f.GeracaoPropria),
            f.Alerta ?? string.Empty));

        return string.Join("\n", new[]
        {
            Linha("UC", "Fatura", "Quota", "Consumo", "Recebido", "Compensado", "Saldo", "Geração própria", "Alerta"),
            Linha("---", "---", "---", "---", "---", "---", "---", "---", "---"),
        }.Concat(linhas));
    }

    public static string Alertas(IReadOnlyList<LinhaFaturaUc> faturas)
    {
        var alertas = faturas
            .Where(f => f.Alerta is not null)
            .Select(f => $"- UC {f.Uc}, fatura {f.Fatura}: {f.Alerta}");

        var corpo = alertas.Any()
            ? string.Join("\n", alertas)
            : "Nenhum alerta de inconsistência.";

        return $"Alertas\n{corpo}";
    }

    // Texto completo do relatório, igual ao que a CLI imprime no terminal.
    public static string Relatorio(RelatorioApuracao relatorio)
    {
        return string.Join("\n\n",
            TabelaUsinas(relatorio.Usinas),
            TabelaFaturasUc(relatorio.FaturasUc),
            Alertas(relatorio.FaturasUc));
    }

    // Uma linha por fatura de UC. Alerta vazio quando a equação fecha.
    public static string Csv(IReadOnlyList<LinhaFaturaUc> faturas)
    {
        var cabecalho = "UC,fatura,quota,consumo,recebido,compensado,saldo,geração própria,alerta";

        var linhas = faturas.Select(f => string.Join(",",
            EscaparCsv(f.Uc),
            EscaparCsv(f.Fatura),
            EscaparCsv(Percentual(f.Quota)),
            EscaparCsv(Valor(f.Consumo)),
            EscaparCsv(Valor(f.Recebido)),
            EscaparCsv(Valor(f.Compensado)),
            EscaparCsv(Valor(f.Saldo)),
            EscaparCsv(Valor(f.GeracaoPropria)),
            EscaparCsv(f.Alerta ?? string.Empty)));

        return string.Join("\n", new[] { cabecalho }.Concat(linhas));
    }

    // Entre aspas só quando o campo tem separador, aspas ou quebra de linha. Aspas internas são duplicadas.
    private static string EscaparCsv(string campo)
    {
        var precisaDeAspas = campo.IndexOfAny([',', ';', '"', '\n', '\r']) >= 0;
        return precisaDeAspas ? $"\"{campo.Replace("\"", "\"\"")}\"" : campo;
    }

    // Monta uma linha da tabela Markdown.
    private static string Linha(params string[] celulas)
    {
        return $"| {string.Join(" | ", celulas)} |";
    }
}
