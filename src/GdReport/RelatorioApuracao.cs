namespace GdReport;

public record LinhaUsina(
    string Instalacao,
    string PeriodoUso,
    decimal Geracao,
    decimal Transferencia,
    decimal CompensacaoPropria,
    decimal SaldoAnterior,
    decimal Saldo);

public record LinhaFaturaUc(
    string Uc,
    string Fatura,
    decimal Quota,
    decimal Consumo,
    decimal Recebido,
    decimal Compensado,
    decimal SaldoAnterior,
    decimal Saldo,
    decimal GeracaoPropria,
    string? Alerta = null);

public record RelatorioApuracao(
    IReadOnlyList<LinhaUsina> Usinas,
    IReadOnlyList<LinhaFaturaUc> FaturasUc);
