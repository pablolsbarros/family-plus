# API local — Sprint 6

Base URL de desenvolvimento: `http://localhost:5160/api`. Todas as respostas usam o envelope `{ "success": true, "data": ... }`; falhas de domínio retornam mensagem e erros de validação.

## Autenticação e cadastros

`GET /auth/setup-status`, `POST /auth/setup`, `POST /auth/login`, `POST /auth/logout` e `GET /auth/me` tratam o acesso local. Após login, envie `X-Session-Token` nas demais rotas.

Membros, contas e categorias seguem os recursos `GET/POST /membros|contas|categorias`, `GET/PUT /{recurso}/{id}` e `PATCH /{recurso}/{id}/status`.

## Movimentações

| Método | Rota | Uso |
|---|---|---|
| GET/POST/PUT | `/transacoes` e `/transacoes/{id}` | Histórico de receitas e despesas |
| PATCH | `/transacoes/{id}/cancelar` | Cancelamento lógico |
| GET/POST/PUT/PATCH | `/receitas`, `/despesas`, `/transferencias` | Atalhos tipados e transferências atômicas |
| GET | `/contas/{id}/saldo`, `/contas/{id}/extrato` | Saldo derivado e histórico |
| GET | `/movimentacoes/resumo` | Consumo e resultado sem duplicar pagamento de fatura |

## Cartões

| Método | Rota | Uso |
|---|---|---|
| GET/POST | `/cartoes` | Lista e cria cartões |
| GET/PUT | `/cartoes/{id}` | Consulta e edita cartão |
| PATCH | `/cartoes/{id}/status` | Ativa/desativa cartão |
| GET | `/cartoes/{id}/limite` | Limites total, utilizado e disponível derivados |
| GET | `/cartoes/{id}/faturas` | Faturas do cartão |
| GET | `/cartoes/{id}/projecao-faturas` | Parcelas e faturas futuras em aberto |

O corpo de cartão contém `membroId`, `nome`, `bandeira`, `ultimosDigitos`, `limiteTotalCentavos`, `diaFechamento`, `diaVencimento`, `contaPagamentoPadraoId` e `observacao` opcional.

## Compras e parcelamentos

| Método | Rota | Uso |
|---|---|---|
| GET/POST | `/compras-cartao` | Lista e cria compra à vista ou parcelada |
| GET | `/compras-cartao/{id}` | Consulta compra e parcelas |
| POST | `/compras-cartao/{id}/cancelar` | Cancela compra ainda não fechada/paga |
| POST | `/compras-cartao/{id}/estornar` | Estorno total ou parcial |

Exemplo de criação:

```json
{
  "cartaoId": "uuid",
  "membroId": "uuid",
  "categoriaId": "uuid",
  "descricao": "Notebook",
  "valorTotalCentavos": 359999,
  "quantidadeParcelas": 10,
  "dataCompra": "2026-09-13T12:00:00Z"
}
```

O estorno recebe `valorCentavos`, `descricao`, `dataEstorno` e `faturaId` opcional.

## Faturas

| Método | Rota | Uso |
|---|---|---|
| GET | `/faturas` e `/faturas/{id}` | Lista e detalha faturas com itens |
| POST | `/faturas/{id}/fechar` | Fecha manualmente uma fatura aberta |
| POST | `/faturas/{id}/pagar` | Paga integralmente uma fatura fechada/vencida |

O pagamento recebe `contaPagamentoId`, `dataPagamento` e `valorCentavos`. Nesta Sprint, o valor deve ser igual ao total da fatura: pagamentos parciais são evolução planejada.

Swagger em `/swagger` expõe o contrato executável completo.

## Recorrências, ocorrências e assinaturas

| Método | Rota | Uso |
|---|---|---|
| GET/POST | `/recorrencias` | Lista e cria receitas/despesas recorrentes |
| GET | `/recorrencias/receitas`, `/recorrencias/despesas`, `/recorrencias/proximas`, `/recorrencias/resumo` | Filtros, vencimentos e projeção |
| GET/PUT | `/recorrencias/{id}` | Consulta e edita a regra; a edição recria somente previsões futuras |
| PATCH | `/recorrencias/{id}/pausar`, `/reativar`, `/encerrar` | Controla o ciclo da recorrência |
| GET | `/ocorrencias-recorrentes` | Lista previsões por data, status, tipo ou recorrência |
| POST | `/ocorrencias-recorrentes/{id}/efetivar` | Cria a transação efetivada e vincula a ocorrência |
| PATCH/PUT | `/ocorrencias-recorrentes/{id}/ignorar`, `/ocorrencias-recorrentes/{id}` | Ignora ou cria exceção de valor/data |
| GET/POST | `/assinaturas` | Lista e cria assinaturas |
| GET | `/assinaturas/resumo` | Custo mensal/anual equivalente e quantidade ativa |
| PUT/PATCH | `/assinaturas/{id}`, `/assinaturas/{id}/status` | Edita, cancela ou reativa sem apagar histórico |

`RecurrenceRequest` recebe membro, conta, categoria, `tipo` (`RECEITA` ou `DESPESA`), descrição, valor em centavos, frequência, período, geração automática, classificação (`NORMAL` ou `CONTA_FIXA`) e observação opcional. `SubscriptionRequest` recebe periodicidade e método de pagamento (`CONTA` ou `CARTAO`); cartão é obrigatório quando o método é `CARTAO`.

## Dashboard

Todas as rotas abaixo aceitam `dataInicio`, `dataFim`, `membroId` e `horizonteDias`. O horizonte padrão é 30 dias e aceita 7, 15, 30, 60 ou 90 dias.

| Método | Rota | Uso |
|---|---|---|
| GET | `/dashboard/resumo` | Consulta agregada usada pela visão geral |
| GET | `/dashboard/saldo`, `/dashboard/saldos-contas` | Saldo consolidado e por conta ativa |
| GET | `/dashboard/receitas`, `/dashboard/despesas`, `/dashboard/resultado` | Valores do período selecionado |
| GET | `/dashboard/fluxo-caixa` | Eventos realizados, previstos, faturas e saldo acumulado |
| GET | `/dashboard/saldo-projetado` | Receitas/despesas previstas, faturas e saldo final do horizonte |
| GET | `/dashboard/proximas-receitas`, `/dashboard/proximas-despesas` | Compromissos futuros classificados por tipo |
| GET | `/dashboard/cartoes` | Fatura atual, limite, utilização e disponibilidade |
| GET | `/dashboard/despesas-categorias`, `/dashboard/receitas-categorias` | Agregação por categoria e percentual |
| GET | `/dashboard/evolucao` | Evolução dos seis meses recentes |
| GET | `/dashboard/kpis` | Taxa de poupança, comprometimento, gastos fixos e próximos totais |

O Dashboard é uma camada de consulta: não grava totais calculados. Receitas e despesas do período consideram transações efetivadas e consumo de cartão, excluindo transferências e a transação técnica de pagamento de fatura.

## Planejamento e orçamento

| Método | Rota | Uso |
|---|---|---|
| GET/POST | `/orcamentos` | Lista por ano/mês/membro/status e cria orçamento mensal |
| GET/PUT | `/orcamentos/{id}` | Consulta e atualiza o cabeçalho enquanto não estiver encerrado |
| POST | `/orcamentos/{id}/ativar`, `/encerrar`, `/reabrir` | Controla o ciclo do mês e preserva o histórico |
| GET/POST | `/orcamentos/{id}/itens` | Lista e adiciona limites por categoria/membro |
| PUT/DELETE | `/orcamentos/{id}/itens/{itemId}` | Edita ou arquiva logicamente um item |
| POST | `/orcamentos/{id}/copiar` | Copia categorias, membros e valores para outro mês; aceita `reajustePercentual` |
| GET | `/orcamentos/{id}/realizado`, `/resumo` | Planejado, realizado, disponível, uso, margem e poupança planejada |
| GET | `/orcamentos/{id}/comparativo` | Variação absoluta e percentual por categoria |
| GET | `/orcamentos/{id}/projecao` | Realizado mais despesas futuras conhecidas e estouro projetado |
| GET | `/dashboard/orcamento` | Orçamento ativo do mês e categorias próximas do limite |

O corpo de criação contém `ano`, `mes`, `membroId` opcional (nulo representa a família), `descricao`, `receitaPrevistaCentavos`, `modoReceitaPrevista` (`MANUAL` nesta sprint), `status` e `observacao`. Cada item contém `categoriaId`, `membroId` opcional, `valorPlanejadoCentavos` e `observacao`.

O realizado usa `DataCompetencia` para transações comuns e `DataCompra` para compras de cartão. Somente despesas efetivadas e compras ativas de cartão são consumo; transferências e `PAGAMENTO_FATURA` são excluídos. Categoria pai inclui suas filhas, mas o serviço rejeita orçamento pai e filho no mesmo ramo para impedir dupla contagem. Categorias sem item aparecem como `SEM_ORCAMENTO` e alimentam alertas.

## Alertas

| Método | Rota | Uso |
|---|---|---|
| GET | `/alertas` | Lista alertas por tipo, severidade e situação resolvida |
| POST | `/alertas/{id}/lido` | Marca o alerta como lido |
| POST | `/alertas/{id}/resolver` | Resolve o alerta |

Os tipos disponíveis são `CONTA_VENCIDA`, `CONTA_PROXIMA`, `FATURA_PROXIMA`, `FATURA_VENCIDA`, `SALDO_NEGATIVO`, `SALDO_PROJETADO_NEGATIVO`, `LIMITE_CARTAO_ALTO`, `RECORRENCIA_ATRASADA`, `ORCAMENTO_ATENCAO`, `ORCAMENTO_CRITICO`, `ORCAMENTO_ESTOURADO`, `ORCAMENTO_ESTOURO_PROJETADO` e `CATEGORIA_SEM_ORCAMENTO`. A geração automática usa uma chave estável por condição para impedir duplicidade na inicialização ou em novas consultas.
