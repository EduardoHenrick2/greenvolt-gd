# Log de Interações com IA

## Interação 1 — Parser Numérico (`Numero.cs` e `NumeroTests.cs`)
- **Pedido:** Criar parser numérico em `decimal` guiado por testes (TDD) capaz de interpretar formatos misturados do XML da CEMIG (`2.250,00` e `8000.0`) pelo conteúdo da string, sem depender da cultura do sistema operacional e sem arredondar.
- **Sugestão e Alinhamento com a IA:** A IA levantou casos de borda antes de codificar: tratamento de entradas alfabéticas inválidas (`"abc"`), pontos posicionados após a vírgula (`"1.234,5.6"`), números negativos e limitação do `[InlineData]` do C# para o tipo `decimal`.
- **Correção / Decisão Humana:**
  1. Definido que entradas inválidas ou com ponto após a vírgula devem lançar `FormatException` (abordagem *Fail-Fast* para não mascarar lixo no XML como zero), enquanto `null`/vazio retornam `0m`.
  2. Rejeitada a adição de bibliotecas externas de Property-Based Testing (FsCheck) para manter o projeto enxuto conforme o `AGENTS.md`.
  3. Aceita a troca de `[InlineData]` por `[MemberData]` com `TheoryData<string, decimal>`, pois atributos C# não aceitam `decimal` em tempo de compilação.
- **Verificação:** Executado `dotnet test` validando cenários válidos, vazios, negativos, espaços nas bordas, independência de cultura (`de-DE`) e disparo de `FormatException`. Todos os testes passaram.

## Interação 2 — Modelo Bruto e Leitor de XML (`RegistroGd.cs`, `LeitorXml.cs` e `LeitorXmlTests.cs`)
- **Pedido:** Implementar o `record RegistroGd` e a classe `LeitorXml` com testes TDD no xUnit, garantindo a soma das duas tags `<Qtd_compensacao>` na mesma linha via `Elements()` e integração com `Numero.Parse`.
- **Sugestão e Alinhamento com a IA:** Como a pasta `data/` ainda não continha o XML físico, a IA implementou a detecção de registros buscando elementos que possuem o nó filho `Instalacao` (tornando o leitor agnóstico ao nome da tag de linha) e alertou sobre a necessidade de validar possíveis *XML Namespaces* quando o arquivo real for adicionado.
- **Correção / Decisão Humana:**
  1. Validado o uso estrito de `linha.Elements("Qtd_compensacao").Sum(e => Numero.Parse(e.Value))` para somar ponta e fora de ponta na mesma linha sem misturar linhas distintas.
  2. Mantida a separação entre `LerXml(string)` e `LerArquivo(string)` para permitir testes unitários em memória e leitura de arquivo temporário.
- **Verificação:** Executado `dotnet test` no terminal local; todos os 28 testes (20 de `NumeroTests` + 8 de `LeitorXmlTests`) passaram.

## Interação 3 — Integração do LeitorXml com o XML Real (`data/Relatorio_GD_UFV_Exemplo_2026-09.xml`)
- **Pedido:** Validar `LeitorXml` contra o arquivo XML real da CEMIG recém-adicionado em `data/` e criar testes de integração.
- **Sugestão e Alinhamento com a IA:** Ao rodar os novos testes contra o XML real, o teste falhou inicialmente com `FormatException` porque a tag `<Quota>` vem formatada com símbolo de porcentagem (`20,00000%`). A IA propôs tratar o sufixo `%` especificamente na leitura da Quota em `LeitorXml.cs` (mantendo `Numero.Parse` estrito) e armazenar o valor em pontos percentuais (`20m`).
- **Correção / Decisão Humana:**
  1. Aprovado manter `Numero.Parse` puro/estrito e remover o `%` apenas no método auxiliar de percentual em `LeitorXml.cs` (respeitando o princípio de responsabilidade única).
  2. Definido manter `Quota` em pontos percentuais (`20m` para `20%`), facilitando a exibição nos relatórios e a conferência de soma igual a 100%.
  3. Mantidos os testes extras de integração que validam as 14 linhas do XML real, encoding de caracteres acentuados (`Geração`) e soma real de `Qtd_compensacao` (`7400.0 + 500.0 = 7900`).
- **Verificação:** Executado `dotnet test` com todos os testes (unitários e de integração com o XML real) passando.

## Interação 4 — Modelos de Saída e Apuração da Usina Geradora (`RelatorioApuracao.cs`, `ProcessadorRelatorio.cs` e `ProcessadorRelatorioTests.cs`)
- **Pedido:** Criar os `records` imutáveis de saída (`LinhaUsina`, `LinhaFaturaUc`, `RelatorioApuracao`) e implementar a separação e apuração da usina geradora em `ProcessadorRelatorio.Processar` com TDD.
- **Sugestão e Alinhamento com a IA:** A IA implementAs três decisões que o Kiro tomou na Parte 2 estão **100% corretas e alinhadas com as regras de negócio**. Especialmente o terceiro ponto: no arquivo da CEMIG, a UC `7000110055` tem geração própria (então sua modalidade traz tanto "Geradora" quanto "Recebedora"), e o teste que ele criou garante que ela não seja confundida com a usina principal (`3010000000`).

Abaixo estão o roteiro de explicação desta etapa para a sua entrevista, o registro do `ia-log.md`, o commit e o prompt da **Fase 3 — Parte 3 (Agrupamento das UCs e Validação do Gabarito)**.

---

**1. Roteiro para a Apresentação de 20 Minutos (Como explicar a Parte 2)**

* **Filtro da Usina vs. UC com Geração Própria:**
  * *"Para isolar a usina (`3010000000`), filtramos registros onde `Modalidade` contém `'Geradora'` e **não** contém `'Recebedora'` (usando `StringComparison.OrdinalIgnoreCase`). Essa segunda condição é essencial porque uma unidade consumidora pode ter microgeração própria e aparecer como Geradora/Recebedora simultaneamente — como acontece na UC `7000110055` — devendo permanecer na tabela de clientes."*
* **Prova Real Física da Usina:**
  * *"Nos testes de integração com o XML real, validamos a identidade física da usina nos dois meses (`2026-08` e `2026-09`): tudo o que a usina gerou (`11.500` e `12.000` kWh) é exatamente igual à soma do que ela transferiu para as UCs (`11.300` e `11.800` kWh) mais a sua compensação própria (`200` kWh)."*

---

**2. Registro para adicionar no seu `docs/ia-log.md`**

Abra o `docs/ia-log.md` e adicione a **Interação 4**:

```markdown
## Interação 4 — Modelos de Saída e Apuração da Usina Geradora (`RelatorioApuracao.cs` e `ProcessadorRelatorio.cs`)
- **Pedido:** Criar os records imutáveis de saída (`LinhaUsina`, `LinhaFaturaUc`, `RelatorioApuracao`) e implementar a separação e cálculo da Usina Geradora com TDD.
- **Sugestão e Alinhamento com a IA:** A IA estruturou o filtro da usina (`Modalidade` com "Geradora" e sem "Recebedora", case-insensitive), aplicou a regra de não somar `Qtd_compensacao` entre linhas do mesmo grupo e utilizou o menor/maior `Periodo` para `SaldoAnterior` e `Saldo`.
- **Correção / Decisão Humana:**
  1. Confirmada a regra de exclusão de "Recebedora" no filtro da usina, garantindo que UCs clientes com geração própria (ex: UC 7000110055) permaneçam na lista de UCs.
  2. Validado no XML real que a usina `3010000000` fecha a equação `Geracao == Transferencia + CompensacaoPropria` nos períodos `2026-08` (11.500 = 11.300 + 200) e `2026-09` (12.000 = 11.800 + 200).
- **Verificação:** Executado `dotnet test` com 38 de 38 testes passando.

## Interação 5 — Agrupamento das UCs Clientes, Ordenação Cronológica e Gabarito (`ProcessadorRelatorio.cs`)
- **Pedido:** Implementar a apuração de `FaturasUc` agrupando por `Instalacao + PeriodoUso`, aplicando regras de soma seletiva, cálculo de saldo pelo menor/maior `Periodo` cronológico e validação contra o gabarito do XML real.
- **Sugestão e Alinhamento com a IA:**
  1. Ao cruzar o XML real com o gabarito (`7000110011`), identificou-se que o código da UC vem na tag `<Numero_uc>`. Foi adicionado `string NumeroUc = ""` em `RegistroGd` e atualizado `LeitorXml.cs`.
  2. Na primeira rodada de testes de período inválido, um grupo com 1 único registro não disparava erro porque o `OrderBy` do LINQ otimiza coleções unitárias e não avalia a chave. A IA corrigiu validando todos os períodos antes do agrupamento.
- **Correção / Decisão Humana:**
  1. Aprovada a adição de `NumeroUc` com valor padrão para preservar a assinatura dos testes anteriores.
  2. Mantida a validação estrita (`FormatException`) para `Periodo` e `PeriodoUso` inválidos ou vazios (*Fail-Fast*).
  3. Validado o caso real da UC `7000110022` (`2026-09`), que possui duas linhas na mesma fatura (`Recebido = 3955 + 4130 = 8085`, `Consumo = 8000` sem somar, `SaldoAnterior = 300`, `Saldo = 485`), além da UC `7000110011` e da geração própria da UC `7000110055`.
- **Verificação:** Executado `dotnet test` com 48 de 48 testes passando.

## Interação 6 — Detecção de Inconsistências e Geração de Alertas (`ProcessadorRelatorio.cs`)
- **Pedido:** Implementar a verificação de consistência do saldo (`SaldoAnterior + Recebido + GeracaoPropria - Compensado == Saldo`) para cada fatura de UC, mantendo o valor do arquivo e gerando alerta formatado em caso de divergência.
- **Sugestão e Alinhamento com a IA:**
  1. A IA propôs não arredondar casas decimais significativas dentro do texto do alerta (removendo apenas zeros à direita) para evitar que divergências fracionárias pequenas fossem exibidas como "diferença 0".
  2. Ajustou o saldo final do mock de um teste unitário da Parte 3 para `215` a fim de fechar a equação naquele cenário onde o foco era apenas testar a soma de recebimentos.
- **Correção / Decisão Humana:**
  1. Aprovada a preservação de casas decimais significativas no alerta e formatação pt-BR determinística (independente da cultura do SO).
  2. Validados no XML real os 3 alertas esperados (UC 7000110033 nas faturas 2026-09 e 2026-10, e UC 7000110044 na fatura 2026-09) e as 8 faturas sem alerta (`Alerta == null`).
- **Verificação:** Executado `dotnet test` com 53 de 53 testes passando.