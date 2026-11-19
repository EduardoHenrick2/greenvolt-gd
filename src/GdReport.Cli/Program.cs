using System.Text;
using System.Xml;
using GdReport;

const string Uso = "Uso: GdReport.Cli <arquivo.xml> [--md <saida.md>] [--csv <saida.csv>] [--whatsapp [saida.txt]]";

if (args.Length == 0)
{
    Console.Error.WriteLine(Uso);
    return 1;
}

var caminho = args[0];
string? caminhoMd = null;
string? caminhoCsv = null;
var gerarWhatsapp = false;
string? caminhoWhatsapp = null;
var indice = 1;
while (indice < args.Length)
{
    var flag = args[indice];
    if (flag == "--whatsapp")
    {
        gerarWhatsapp = true;
        var temCaminho = indice + 1 < args.Length && !args[indice + 1].StartsWith("--", StringComparison.Ordinal);
        caminhoWhatsapp = temCaminho ? args[indice + 1] : null;
        indice += temCaminho ? 2 : 1;
        continue;
    }
    if (indice + 1 >= args.Length || flag is not ("--md" or "--csv"))
    {
        Console.Error.WriteLine(Uso);
        return 1;
    }
    if (flag == "--md")
    {
        caminhoMd = args[indice + 1];
    }
    else
    {
        caminhoCsv = args[indice + 1];
    }
    indice += 2;
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

    if (gerarWhatsapp)
    {
        var configuracao = ConfiguracaoLlm.DeVariaveisAmbiente();
        IClienteLlm? cliente = configuracao is null ? null : new ClienteLlmHttp(configuracao);
        var resultado = await OrquestradorMensagemWhatsapp.GerarAsync(relatorio, cliente);
        Console.WriteLine();
        Console.WriteLine("--- Mensagem WhatsApp ---");
        Console.WriteLine();
        Console.WriteLine(resultado.Mensagem);
        Console.WriteLine();
        Console.WriteLine("-------------------------");
        Console.WriteLine();
        Console.WriteLine(resultado.Conferida
            ? $"[Auditoria WhatsApp] OK - 100% dos números conferidos | Origem: {resultado.Origem}"
            : $"[Auditoria WhatsApp] ALERTA - divergência detectada | Origem: {resultado.Origem}");
        if (caminhoWhatsapp is not null)
        {
            File.WriteAllText(caminhoWhatsapp, resultado.Mensagem + "\n", new UTF8Encoding(false));
        }
    }
    return 0;
}
catch (Exception ex) when (ex is XmlException or FormatException)
{
    Console.Error.WriteLine($"Erro ao ler '{caminho}': {ex.Message}");
    return 1;
}
