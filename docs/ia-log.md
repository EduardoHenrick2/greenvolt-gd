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