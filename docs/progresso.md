# Progresso do Desenvolvimento

## Concluído
- [x] Setup da solução .NET 10 (`GdReport`, `GdReport.Cli`, `GdReport.Tests`) e `.gitignore`.
- [x] Definição de diretrizes em `AGENTS.md` e decisões de arquitetura em `docs/decisoes.md`.
- [x] **Fase 2:** `Numero.cs`, `RegistroGd.cs`, `LeitorXml.cs` e testes unitários.
- [x] **Fase 3:** Separação da usina geradora, agrupamento das 11 faturas de UC por `Instalacao + PeriodoUso`, ordenação cronológica de `Periodo` e cálculo de saldos.
- [x] **Fase 4:** Detecção de inconsistências de saldo, geração dos 3 alertas reais e validação completa do gabarito de 11 linhas, quotas (100%) e rateio.

## Em Andamento / Próximo Passo
- [ ] **Fase 5:** Implementar `src/GdReport.Cli` e geração de `docs/saida-exemplo.md` e `docs/saida-exemplo.csv`.

## Pendente
- [ ] **Fase 7:** Documentação completa no `README.md`, revisão de clone limpo e entrega antes de 19/11 às 18h.
- [ ] **Fase 6 (Opcional/Bônus):** Mensagem de WhatsApp com LLM e conferência automática dos números.