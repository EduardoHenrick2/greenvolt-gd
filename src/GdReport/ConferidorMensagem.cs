using System.Text.RegularExpressions;
namespace GdReport;
public record ResultadoConferencia(bool Valido, IReadOnlyList<string> Divergencias);
public static class ConferidorMensagem
{
    private static readonly Regex Datas = new(@"\b\d{4}-\d{2}\b|\b\d{2}/\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex Numeros = new(@"\d+(?:\.\d{3})*(?:,\d+)?%?", RegexOptions.Compiled);
    public static ResultadoConferencia Conferir(string mensagem, RelatorioApuracao relatorio)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            return new ResultadoConferencia(false, ["Mensagem vazia."]);
        }
        var oficiais = NumerosOficiais(relatorio);
        var divergencias = ExtrairNumeros(mensagem, relatorio)
            .Where(n => !oficiais.Contains(ParseTexto(n)))
            .Select(n => $"Número divergente: {n}")
            .Distinct()
            .ToList();
        return new ResultadoConferencia(divergencias.Count == 0, divergencias);
    }
    // O regex só aceita pontos como separador de milhar, mas Numero.Parse trata ponto sem vírgula como decimal.
    private static decimal ParseTexto(string numero)
    {
        return Numero.Parse(numero.TrimEnd('%').Replace(".", string.Empty));
    }

    // O sinal é ignorado na conferência: cada valor oficial entra com o seu módulo.
    private static HashSet<decimal> NumerosOficiais(RelatorioApuracao relatorio)
    {
        var valores = new List<decimal>();
        foreach (var u in relatorio.Usinas)
        {
            valores.AddRange([u.Geracao, u.Transferencia, u.CompensacaoPropria, u.SaldoAnterior, u.Saldo]);
        }
        foreach (var f in relatorio.FaturasUc)
        {
            valores.AddRange([f.Quota, f.Consumo, f.Recebido, f.Compensado, f.SaldoAnterior, f.Saldo, f.GeracaoPropria]);
            if (f.Alerta is not null)
            {
                var calculado = f.SaldoAnterior + f.Recebido + f.GeracaoPropria - f.Compensado;
                valores.AddRange([calculado, f.Saldo, Math.Abs(calculado - f.Saldo)]);
            }
        }
        var comAlerta = relatorio.FaturasUc.Count(f => f.Alerta is not null);
        valores.AddRange([relatorio.FaturasUc.Count, relatorio.FaturasUc.Count - comAlerta, comAlerta, relatorio.Usinas.Count]);
        return valores.Select(Math.Abs).ToHashSet();
    }
    private static IEnumerable<string> ExtrairNumeros(string mensagem, RelatorioApuracao relatorio)
    {
        var identificadores = relatorio.Usinas.Select(u => u.Instalacao)
            .Concat(relatorio.FaturasUc.Select(f => f.Uc))
            .Where(id => id.Length > 0)
            .Distinct();
        var texto = mensagem;
        foreach (var id in identificadores)
        {
            texto = texto.Replace(id, " ");
        }
        texto = Datas.Replace(texto, " ");
        return Numeros.Matches(texto).Select(m => m.Value);
    }
}
