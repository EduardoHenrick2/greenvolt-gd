using System.Xml.Linq;

namespace GdReport;

public static class LeitorXml
{
    public static IReadOnlyList<RegistroGd> LerXml(string xml)
    {
        return Ler(XDocument.Parse(xml));
    }

    public static IReadOnlyList<RegistroGd> LerArquivo(string caminho)
    {
        return Ler(XDocument.Load(caminho));
    }

    private static IReadOnlyList<RegistroGd> Ler(XDocument doc)
    {
        return doc.Descendants()
            .Where(e => e.Element("Instalacao") is not null)
            .Select(CriarRegistro)
            .ToList();
    }

    private static RegistroGd CriarRegistro(XElement linha)
    {
        return new RegistroGd(
            Instalacao: Texto(linha, "Instalacao"),
            Modalidade: Texto(linha, "Modalidade"),
            Periodo: Texto(linha, "Periodo"),
            PeriodoUso: Texto(linha, "Periodo_uso"),
            Quota: Percentual(linha, "Quota"),
            QtdConsumo: Decimal(linha, "Qtd_consumo"),
            QtdRecebimento: Decimal(linha, "Qtd_recebimento"),
            QtdTransferencia: Decimal(linha, "Qtd_transferencia"),
            QtdCompensacao: linha.Elements("Qtd_compensacao").Sum(e => Numero.Parse(e.Value)),
            QtdGeracao: Decimal(linha, "Qtd_geracao"),
            QtdSaldoAnt: Decimal(linha, "Qtd_saldo_ant"),
            QtdSaldoAtual: Decimal(linha, "Qtd_saldo_atual"));
    }

    private static string Texto(XElement linha, string tag)
    {
        return linha.Element(tag)?.Value ?? string.Empty;
    }

    private static decimal Decimal(XElement linha, string tag)
    {
        return Numero.Parse(linha.Element(tag)?.Value);
    }

    // No XML real a quota vem como "20,00000%". O valor é mantido em pontos percentuais (20), sem dividir por 100.
    private static decimal Percentual(XElement linha, string tag)
    {
        return Numero.Parse(linha.Element(tag)?.Value?.Replace("%", string.Empty));
    }
}
