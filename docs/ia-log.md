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
- **Sugestão e Alinhamento com a IA:**
  1. A IA estruturou o filtro da usina (`Modalidade` contendo "Geradora" e sem "Recebedora", sem diferenciar maiúsculas) e o agrupamento por `Instalacao + PeriodoUso`, somando `QtdGeracao` e `QtdTransferencia` entre linhas.
  2. Propôs não somar `Qtd_compensacao` entre linhas do mesmo grupo (`CompensacaoPropria` é valor da fatura inteira) e usar o menor/maior `Periodo` para `SaldoAnterior` e `Saldo`.
  3. Propôs que uma linha cuja `Modalidade` contenha as duas palavras ("Geradora" e "Recebedora") não seja tratada como usina e vá para as UCs, coberto por um teste com dados sintéticos.
- **Correção / Decisão Humana:**
  1. Confirmadas as três decisões acima.
  2. No XML real, a única usina é a instalação `3010000000` ("Geração compartilhada - Geradora"). Todas as UCs, inclusive a `7000110055`, têm `Modalidade` "Geração compartilhada - Recebedora". A geração própria da UC `7000110055` (280 em 2026-08 e 300 em 2026-09) vem da tag `Qtd_geracao`, entra no cálculo do saldo e aparece na coluna `geração própria` (implementado na Interação 5).
  3. Validado no XML real que a usina fecha a equação `Geracao == Transferencia + CompensacaoPropria` nos períodos `2026-08` (11.500 = 11.300 + 200) e `2026-09` (12.000 = 11.800 + 200).
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

## Interação 7 — Auditoria Completa do Gabarito (11 Linhas), Quotas e Invariante de Rateio (`ProcessadorRelatorioTests.cs`)
- **Pedido:** Adicionar testes de integração contra o XML real (`data/Relatorio_GD_UFV_Exemplo_2026-09.xml`) validando todas as 11 linhas do gabarito, soma das quotas igual a 100% e a regra `Recebido == Transferencia × Quota`.
- **Sugestão e Alinhamento com a IA:**
  1. A IA comparou o resultado completo das 11 `LinhaFaturaUc` (incluindo `SaldoAnterior`) usando igualdade estrutural de `record`.
  2. Demonstrou que nas linhas recebedoras `Qtd_transferencia` é `0` e o período de rateio segue `Periodo` (não `Periodo_uso`), validando `QtdRecebimento == TransferenciaUsina * (Quota / 100m)` nos períodos `2026-08` e `2026-09` (ex.: UC `7000110011` em `2026-08`: `11.300 × 20% = 2.260`).
  3. Apontou a duplicação do filtro da usina em um helper privado de teste como ponto de atenção para manutenção futura.
- **Correção / Decisão Humana:**
  1. Validada a leitura de rateio por `Periodo` usando a transferência da usina e soma de quotas igual a `100m` por período (com verificação explícita da existência dos dois períodos).
  2. Mantido o teste explícito confirmando `Qtd_transferencia == 0` nas linhas recebedoras para documentar a estrutura do XML da CEMIG.
- **Verificação:** Executado `dotnet test` com todos os testes passando (gabarito de 11 linhas, quotas e rateio verdes).

## Interação 8 — CLI, Formatação de Tabela/Alertas e Tratamento de Erros (`Program.cs`, `FormatadorRelatorio.cs` e `FormatadorRelatorioTests.cs`)
- **Pedido:** Implementar a aplicação de console em `src/GdReport.Cli` aceitando o caminho do XML via argumento, imprimindo a tabela e os alertas no terminal e criando testes de formatação.
- **Sugestão e Alinhamento com a IA:**
  1. A IA centralizou a formatação numérica em `FormatadorRelatorio.Valor` (reutilizada também na geração dos alertas) e configurou `Console.OutputEncoding = Encoding.UTF8`.
  2. Acrescentou tratamento para XML malformado e valores inválidos retornando exit code `1` sem stack trace, e alertou sobre o ruído do MSBuild no `dotnet run`.
- **Correção / Decisão Humana:**
  1. Mantidos os códigos de saída `0` (sucesso) e `1` (argumento ausente, arquivo inexistente ou entrada inválida/malformada) e a tabela de 9 colunas (`UC`, `fatura`, `quota`, `consumo`, `recebido`, `compensado`, `saldo`, `geração própria`, `alerta`).
- **Verificação:** Executado `dotnet test` com 69 de 69 testes passando e validada a execução do CLI sobre `data/Relatorio_GD_UFV_Exemplo_2026-09.xml`.

## Interação 9 — Exportação CSV/Markdown, Flags `--md`/`--csv` e Geração de `docs/saida-exemplo.*` (`FormatadorRelatorio.cs`, `Program.cs`, `FormatadorRelatorioTests.cs`)
- **Pedido:** Implementar a geração de saída CSV com o cabeçalho exigido e gerar os arquivos `docs/saida-exemplo.md` e `docs/saida-exemplo.csv` a partir do mesmo processamento do XML de exemplo.
- **Sugestão e Alinhamento com a IA:**
  1. A IA unificou a geração do console e de `docs/saida-exemplo.md` em `FormatadorRelatorio.Relatorio` e adicionou `--md` e `--csv` opcionais na CLI.
  2. Aplicou escaping de campos com vírgula, ponto e vírgula, aspas ou quebra de linha no CSV, manteve `alerta` vazio nas 8 linhas sem divergência e gravou o CSV em UTF-8 com BOM para correta leitura em planilhas pt-BR.
- **Correção / Decisão Humana:**
  1. Mantida a formatação numérica em pt-BR no CSV para consistência direta com a tabela exibida e a conferência em planilha.
  2. Mantido o CSV focado nas 11 faturas de UC e a usina exibida separadamente no relatório Markdown/console.
- **Verificação:** Executado `dotnet test` com 69+ testes passando e conferidos `docs/saida-exemplo.md` e `docs/saida-exemplo.csv` gerados a partir de `data/Relatorio_GD_UFV_Exemplo_2026-09.xml`.
