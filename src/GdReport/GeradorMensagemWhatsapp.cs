namespace GdReport;
public interface IClienteLlm
{
    Task<string> GerarTextoAsync(string prompt, CancellationToken cancellationToken = default);
}
// Só monta textos a partir do relatório já apurado. Não calcula nada.
public static class GeradorMensagemWhatsapp
{
    public static string MontarPrompt(RelatorioApuracao relatorio)
    {
        return string.Join("\n", [
            "Você é um assistente que redige mensagens de WhatsApp sobre a apuração de geração distribuída (GD).",
            "Redija uma mensagem curta de WhatsApp em português, usando *negrito* para destaques e bullets simples (-).",
            "Regras obrigatórias:",
            "- Não recalcule nada.",
            "- Não invente nenhum número que não esteja nos dados abaixo.",
            "- Use os números exatamente como aparecem nos dados, inclusive os textos dos alertas.",
            "",
            DadosOficiais(relatorio)]);
    }
    public static string MontarPromptCorrecao(
        RelatorioApuracao relatorio, string rascunhoAnterior, IReadOnlyList<string> divergencias)
    {
        return string.Join("\n", [
            "O rascunho anterior da mensagem de WhatsApp contém números que não existem nos dados oficiais.",
            "",
            "Rascunho anterior:",
            rascunhoAnterior,
            "",
            "Divergências encontradas:",
            .. divergencias.Select(d => $"- {d}"),
            "",
            "Corrija o rascunho mantendo apenas os números oficiais abaixo. Não recalcule nada e não invente números.",
            "Mantenha uma mensagem curta de WhatsApp, com *negrito* e bullets simples (-).",
            "",
            DadosOficiais(relatorio)]);
    }
    public static string GerarFallbackDeterministico(RelatorioApuracao relatorio)
    {
        var linhas = new List<string> { "*Apuração de geração distribuída*" };
        foreach (var u in relatorio.Usinas)
        {
            linhas.Add($"*Usina {u.Instalacao}* ({u.PeriodoUso})");
            linhas.Add($"- Geração: {FormatadorRelatorio.Valor(u.Geracao)}");
            linhas.Add($"- Transferência: {FormatadorRelatorio.Valor(u.Transferencia)}");
            linhas.Add($"- Compensação própria: {FormatadorRelatorio.Valor(u.CompensacaoPropria)}");
            linhas.Add($"- Saldo anterior: {FormatadorRelatorio.Valor(u.SaldoAnterior)}");
            linhas.Add($"- Saldo: {FormatadorRelatorio.Valor(u.Saldo)}");
        }
        var comAlerta = relatorio.FaturasUc.Where(f => f.Alerta is not null).ToList();
        linhas.Add($"*Faturas:* {relatorio.FaturasUc.Count} no total, {relatorio.FaturasUc.Count - comAlerta.Count} sem alerta e {comAlerta.Count} com alerta.");
        if (comAlerta.Count == 0)
        {
            linhas.Add("Nenhum alerta encontrado.");
        }
        else
        {
            linhas.Add("*Alertas*");
            linhas.AddRange(comAlerta.Select(f => $"- UC {f.Uc}, fatura {f.Fatura}: {f.Alerta}"));
        }
        return string.Join("\n", linhas);
    }
    private static string DadosOficiais(RelatorioApuracao relatorio)
    {
        var comAlerta = relatorio.FaturasUc.Where(f => f.Alerta is not null).ToList();
        var linhas = new List<string> { "Dados oficiais:", $"Usinas: {relatorio.Usinas.Count}" };
        linhas.AddRange(relatorio.Usinas.Select(u =>
            $"- Usina {u.Instalacao}, período de uso {u.PeriodoUso}: geração {FormatadorRelatorio.Valor(u.Geracao)}, " +
            $"transferência {FormatadorRelatorio.Valor(u.Transferencia)}, " +
            $"compensação própria {FormatadorRelatorio.Valor(u.CompensacaoPropria)}, " +
            $"saldo anterior {FormatadorRelatorio.Valor(u.SaldoAnterior)}, saldo {FormatadorRelatorio.Valor(u.Saldo)}"));
        linhas.Add($"Total de faturas: {relatorio.FaturasUc.Count}");
        linhas.Add($"Faturas sem alerta: {relatorio.FaturasUc.Count - comAlerta.Count}");
        linhas.Add($"Faturas com alerta: {comAlerta.Count}");
        linhas.AddRange(comAlerta.Select(f => $"- Alerta UC {f.Uc}, fatura {f.Fatura}: {f.Alerta}"));
        return string.Join("\n", linhas);
    }
}