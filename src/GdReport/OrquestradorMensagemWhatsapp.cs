namespace GdReport;
public record ResultadoMensagemWhatsapp(string Mensagem, string Origem, int Tentativas, bool Conferida);
public static class OrquestradorMensagemWhatsapp
{
    private const string OrigemFallback = "Fallback determinístico";
    // Tentativas conta as chamadas feitas à LLM, inclusive a que falhou.
    public static async Task<ResultadoMensagemWhatsapp> GerarAsync(
        RelatorioApuracao relatorio,
        IClienteLlm? clienteLlm = null,
        int maxTentativas = 2,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTentativas, 1);
        if (clienteLlm is null)
        {
            return Fallback(relatorio, 0);
        }
        var executadas = 0;
        var prompt = GeradorMensagemWhatsapp.MontarPrompt(relatorio);
        while (executadas < maxTentativas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            executadas++;
            string rascunho;
            try
            {
                rascunho = await clienteLlm.GerarTextoAsync(prompt, cancellationToken);
            }
            catch (Exception erro) when (erro is HttpRequestException or InvalidOperationException
                || (erro is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                return Fallback(relatorio, executadas);
            }
            var conferencia = ConferidorMensagem.Conferir(rascunho, relatorio);
            if (conferencia.Valido)
            {
                return new ResultadoMensagemWhatsapp(rascunho, $"LLM (tentativa {executadas}/{maxTentativas})", executadas, true);
            }
            prompt = GeradorMensagemWhatsapp.MontarPromptCorrecao(relatorio, rascunho, conferencia.Divergencias);
        }
        return Fallback(relatorio, executadas);
    }
    private static ResultadoMensagemWhatsapp Fallback(RelatorioApuracao relatorio, int tentativas)
    {
        var mensagem = GeradorMensagemWhatsapp.GerarFallbackDeterministico(relatorio);
        var conferencia = ConferidorMensagem.Conferir(mensagem, relatorio);
        return new ResultadoMensagemWhatsapp(mensagem, OrigemFallback, tentativas, conferencia.Valido);
    }
}