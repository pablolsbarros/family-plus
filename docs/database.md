# Banco de dados — Sprint 6

O banco SQLite fica em `backend/src/FamilyPlus.Api/data/familyplus.db`. Ao iniciar, a API executa `Database.MigrateAsync()` e aplica também as migrations `Sprint4RecorrenciasAssinaturas`, `Sprint5DashboardAlertas` e `Sprint6Planejamento`.

## Tabelas principais

| Tabela | Finalidade |
|---|---|
| `usuario`, `sessao_local` | Administrador local e sessões com token em hash |
| `membro`, `conta`, `categoria` | Cadastros compartilhados |
| `transacao`, `transferencia` | Movimentações bancárias e transferências |
| `cartao` | Cartão de crédito, titular, limite, dias de ciclo e conta padrão |
| `compra_cartao` | Compra no crédito, categoria, valor total e situação |
| `fatura` | Competência, fechamento, vencimento, total e situação |
| `parcela_cartao` | Rateio de compra e vínculo à fatura da competência |
| `estorno_cartao` | Estornos totais ou parciais vinculados a compra e fatura |
| `recorrencia` | Regra de receita/despesa recorrente, período, frequência e situação |
| `ocorrencia_recorrencia` | Previsão individual, status e vínculo opcional à transação efetivada ou compra de cartão |
| `assinatura` | Serviço recorrente, periodicidade, cobrança por conta/cartão e cancelamento lógico |
| `alerta` | Alertas gerenciais persistidos, leitura, resolução e chave de deduplicação |
| `auditoria`, `configuracao` | Rastreabilidade e preferências |

## Relacionamentos de cartões

```text
membro ──< cartao ──< fatura
conta  ──< cartao (conta de pagamento padrão)
cartao ──< compra_cartao ──< parcela_cartao >── fatura
compra_cartao ──< estorno_cartao >── fatura
```

## Convenções e integridade

- Valores monetários são `INTEGER` em centavos; não há `float`.
- Datas são `DateTimeOffset` em UTC. A competência é gravada ao meio-dia UTC para evitar que a data mensal seja exibida no mês anterior em fusos negativos.
- O limite utilizado é calculado a partir de parcelas ativas ainda não quitadas, descontando estornos; não é uma coluna atualizável manualmente.
- Uma compra cria suas parcelas e faturas necessárias em uma transação SQLite única.
- `transacao.origem = PAGAMENTO_FATURA` identifica o débito técnico de pagamento; essa origem não entra novamente no resumo de consumo.
- Exclusões físicas não são utilizadas para dados financeiros. Cancelamentos e estornos preservam o histórico e geram auditoria.
- Índices cobrem cartão, competência/status de fatura, compra/fatura de parcelas e estornos por compra/fatura.
- `ocorrencia_recorrencia` possui índice único por `(recorrencia_id, data_prevista)`, impedindo duplicação na geração automática.
- As previsões são dados de planejamento; somente transações efetivadas participam do saldo bancário real.
- `orcamento` guarda o cabeçalho mensal, receita prevista, membro/escopo e status; `item_orcamento` guarda cada limite por categoria/membro. O realizado, disponível, variação e projeção são calculados sob demanda.
- `item_orcamento` possui índices por orçamento, categoria e membro e índice único `(orcamento_id, categoria_id, membro_id)`. A validação de serviço complementa a unicidade para itens compartilhados nulos no SQLite.
- O Dashboard consulta as entidades operacionais e de planejamento em tempo real e não mantém uma tabela de totais derivados. `alerta` persiste somente condições acionáveis e usa chaves estáveis para deduplicação.
- `alerta` possui índices por data, tipo, severidade, resolução e chave/situação para manter a central rápida em volume pessoal.

Para uma migration futura:

```powershell
dotnet dotnet-ef migrations add NomeDaMudanca --project .\backend\src\FamilyPlus.Api --startup-project .\backend\src\FamilyPlus.Api --output-dir Migrations
```
