using System.Text;
using System.Xml;
using GdReport;

const string Uso = "Uso: GdReport.Cli <arquivo.xml> [--md <saida.md>] [--csv <saida.csv>]";

if (args.Length == 0)
{
    Console.Error.WriteLine(Uso);
    return 1;
}

var caminho = args[0];
string? caminhoMd = null;
string? caminhoCsv = null;

for (var i = 1; i < args.Length; i += 2)
{
    if (i + 1 >= args.Length || args[i] is not ("--md" or "--csv"))
    {
        Console.Error.WriteLine(Uso);
        return 1;
    }

    if (args[i] == "--md")
    {
        caminhoMd = args[i + 1];
    }
    else
    {
        caminhoCsv = args[i + 1];
    }
}

if (!File.Exists(caminho))
{
    Console.Error.WriteLine($"Arquivo não encontrado: {caminho}");
    return 1;
}

Console.OutputEncoding = Encoding.UTF8;

try
{
    var relatorio = ProcessadorRelatorio.Processar(LeitorXml.LerArquivo(caminho));

    Console.WriteLine(FormatadorRelatorio.Relatorio(relatorio));

    if (caminhoMd is not null)
    {
        File.WriteAllText(caminhoMd, FormatadorRelatorio.Relatorio(relatorio) + "\n", new UTF8Encoding(false));
    }

    // Com BOM, o Excel abre o CSV com acentos corretos.
    if (caminhoCsv is not null)
    {
        File.WriteAllText(caminhoCsv, FormatadorRelatorio.Csv(relatorio.FaturasUc) + "\n", new UTF8Encoding(true));
    }

    return 0;
}
catch (Exception ex) when (ex is XmlException or FormatException)
{
    Console.Error.WriteLine($"Erro ao ler '{caminho}': {ex.Message}");
    return 1;
}
