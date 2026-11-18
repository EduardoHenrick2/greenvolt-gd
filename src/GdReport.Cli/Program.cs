using System.Text;
using System.Xml;
using GdReport;

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: GdReport.Cli <arquivo.xml>");
    return 1;
}

var caminho = args[0];

if (!File.Exists(caminho))
{
    Console.Error.WriteLine($"Arquivo não encontrado: {caminho}");
    return 1;
}

Console.OutputEncoding = Encoding.UTF8;

try
{
    var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(caminho));

    Console.WriteLine(FormatadorRelatorio.TabelaUsinas(relatorio.Usinas));
    Console.WriteLine();
    Console.WriteLine(FormatadorRelatorio.TabelaFaturasUc(relatorio.FaturasUc));
    Console.WriteLine();
    Console.WriteLine(FormatadorRelatorio.Alertas(relatorio.FaturasUc));
    return 0;
}
catch (Exception ex) when (ex is XmlException or FormatException)
{
    Console.Error.WriteLine($"Erro ao ler '{caminho}': {ex.Message}");
    return 1;
}
