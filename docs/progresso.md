# Progresso do Desenvolvimento

## Concluído
- [x] **Fase 1:** Setup da solução .NET 10 (`GdReport`, `GdReport.Cli`, `GdReport.Tests`) e `.gitignore`. Diretrizes em `Agents.md` e decisões em `docs/decisoes.md`.
- [x] **Fase 2:** `Numero.cs`, `RegistroGd.cs`, `LeitorXml.cs` e testes unitários.
- [x] **Fase 3:** Separação da usina geradora, agrupamento das 11 faturas de UC por `Instalacao + PeriodoUso`, ordenação cronológica de `Periodo` e cálculo de saldos.
- [x] **Fase 4:** Detecção de inconsistências de saldo, geração dos 3 alertas reais e validação completa do gabarito de 11 linhas, quotas (100%) e rateio.
- [x] **Fase 5:** CLI `src/GdReport.Cli` com códigos de saída `0`/`1`, `FormatadorRelatorio` (tabelas, alertas e CSV), flags `--md`/`--csv` e geração de `docs/saida-exemplo.md` e `docs/saida-exemplo.csv`.

- [x] **Fase 6 (Bônus):** Mensagem de WhatsApp com conferência determinística de números (`ConferidorMensagem`), cliente HTTP nativo para LLM (`ClienteLlmHttp`), orquestrador com re-tentativa e fallback, flag `--whatsapp` e `docs/mensagem-whatsapp-exemplo.txt`. 120 testes passando.

## Em Andamento / Próximo Passo
- [x] **Fase 7 (Parte 1):** `README.md` completo (visão geral, execução, exemplo de saída, decisões e processo com IA).
- [ ] **Fase 7 (Parte 2):** Revisão de clone limpo e entrega antes de 19/11 às 18h.
