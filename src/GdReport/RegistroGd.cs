namespace GdReport;

public record RegistroGd(
    string Instalacao,
    string Modalidade,
    string Periodo,
    string PeriodoUso,
    decimal Quota,
    decimal QtdConsumo,
    decimal QtdRecebimento,
    decimal QtdTransferencia,
    decimal QtdCompensacao,
    decimal QtdGeracao,
    decimal QtdSaldoAnt,
    decimal QtdSaldoAtual);
