# Progresso do Desenvolvimento

## Concluído
- [x] Setup da solução .NET 10 (`GdReport`, `GdReport.Cli`, `GdReport.Tests`) e `.gitignore`.
- [x] Definição de diretrizes em `AGENTS.md` e decisões de arquitetura em `docs/decisoes.md`.
- [x] **Fase 2 (Parte 1):** Implementação de `Numero.cs` e suíte de testes `NumeroTests.cs` cobrindo formatos misturados e validações de borda.

## Em Andamento / Próximo Passo
- [ ] **Fase 2 (Parte 2):** Criar modelo `RegistroGd.cs` e leitor `LeitorXml.cs` (tratando a soma das duas tags `Qtd_compensacao` na mesma linha com `Elements()`).

## Pendente
- [ ] **Fase 3:** Agrupamento por `Instalacao` + `Periodo_uso`, separação da usina e cálculo de saldos.
- [ ] **Fase 4:** Validação completa do gabarito (11 linhas) e geração dos 3 alertas de inconsistência.
- [ ] **Fase 5:** CLI e exportação para `docs/saida-exemplo.md` e `docs/saida-exemplo.csv`.
- [ ] **Fase 7:** Documentação final no `README.md` e revisão de entrega.