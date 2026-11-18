# Registro de Decisões Técnicas e de Negócio

1. **Posto Horário (Decidido):** Não filtrar por `Posto_horario`; somar as duas ocorrências de `Qtd_compensacao` (ponta e fora de ponta) dentro da mesma linha usando `Elements()`.
2. **Formato Numérico (Decidido):** Decidido pelo valor da string (presença de vírgula indica padrão pt-BR com ponto de milhar; ausência de vírgula indica ponto decimal), independente da cultura da máquina (`CultureInfo`).
3. **Precisão e Arredondamento (Decidido):** Todo o processamento é feito em `decimal`, com arredondamento aplicado apenas na exibição/exportação final.
4. **Regra de Saldos (Decidido):** Os saldos seguem o campo `Periodo` da linha (e não `Periodo_uso`). O saldo inicial da fatura é o `Qtd_saldo_ant` do primeiro mês e o saldo final é o `Qtd_saldo_atual` do último mês.
5. **Geração Própria da UC (A confirmar):** A geração própria em unidades consumidoras (ex: UC 7000110055) entra no cálculo do saldo e é exibida em coluna dedicada.
6. **Tratamento de Inconsistências (Decidido):** Inconsistências no XML não são sobrescritas; mantém-se o valor reportado pelo arquivo na tabela principal e emite-se um alerta detalhando a diferença encontrada.