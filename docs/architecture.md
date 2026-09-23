# Arquitetura — Sprint 6

O Family+ é um monólito modular local: React/Vite em `localhost:5173`, API ASP.NET Core em `localhost:5160` e SQLite privado da API. Não existe integração de negócio remota.

```text
React + TypeScript
       │ HTTP local / JSON
       ▼
Controllers → Services → EF Core → SQLite
       │           │
       └───────────┴── Auditoria local
```

## Regras centrais

1. Valores financeiros usam centavos inteiros, UUIDs e timestamps UTC.
2. Saldo bancário é derivado de transações efetivadas; o limite de cartão é derivado de parcelas pendentes.
3. Cartão e conta bancária são conceitos separados: a compra reduz o limite, e somente o pagamento integral de fatura cria uma despesa na conta.
4. `InvoiceCycle` concentra a regra de competência. Compra até o fechamento pertence ao mês corrente; depois dele, ao mês seguinte. A mesma regra é reutilizada para cada parcela.
5. O parcelamento distribui quociente e resto exatamente, adicionando o resíduo à última parcela.
6. Faturas podem estar abertas, fechadas, vencidas, pagas ou canceladas. A inicialização aplica fechamento automático e marca atraso quando aplicável.
7. Resumo de movimentações agrega compras de cartão ativas e exclui a transação técnica de pagamento de fatura, evitando dupla contagem para o consumidor.
8. Cancelamento e estorno são regras transacionais e auditáveis: a fatura é recalculada no mesmo commit.
9. `RecurrenceService` separa planejamento de caixa de realização: ocorrências são previsões e não alteram saldo; a efetivação delega a criação da transação a `TransactionService`.
10. A geração ocorre na inicialização e nas consultas de recorrências. O índice único `(recorrencia_id, data_prevista)` torna o processo idempotente e evita duplicação no horizonte padrão de 12 meses.
11. Alterar uma regra remove e recria somente previsões futuras pendentes/atrasadas. Ocorrências efetivadas, ignoradas e seu vínculo financeiro permanecem como histórico.
12. Assinatura cobrada em conta cria despesa prevista; assinatura em cartão cria uma compra de uma parcela por meio de `CardService`. O custo mensal/anual é derivado, nunca gravado como saldo.
13. `DashboardService` materializa consultas locais e combina saldo de contas, transações, consumo de cartão, faturas e ocorrências recorrentes. A seleção por membro e período ocorre antes das agregações.
14. `AlertService` avalia condições financeiras na inicialização e ao consultar a central. Cada condição possui chave estável; o alerta é atualizado enquanto ativo e não é recriado em duplicidade após novas aberturas.
15. `BudgetService` concentra o cabeçalho, itens, cópia, ciclo de status, cálculo por competência, agregação pai/filha, comparação, projeção e alertas de orçamento. O Dashboard apenas consulta o resumo ativo do mês.
16. O orçamento não duplica dados derivados: transferências e pagamentos de fatura ficam fora do consumo, compras de cartão entram por `DataCompra` e previsões recorrentes entram somente na projeção futura.

## Workspace e navegação centralizada

A interface segue o modelo de workspace: menu lateral para descoberta, gerenciador central de telas para trabalho ativo e abas para alternância sem desmontar as telas abertas. A navegação não é responsabilidade dos módulos individuais.

```text
Registro central de telas
        │ código, rota, módulo, permissão, singleton
        ▼
Gerenciador de workspace ──> abas abertas / aba ativa / recentes / favoritos
        │                                   │
        ├── singleton por funcionalidade     └── sessionStorage/localStorage local
        ▼
Painéis montados e ocultos quando inativos
```

- Cada registro possui nome amigável, código permanente (`DSH001`, `FIN001`, `PLN001` etc.), rota, módulo, descrição, permissão conceitual e `singleton`.
- Ao solicitar uma funcionalidade já aberta, o gerenciador a foca em vez de criar outra aba.
- O Dashboard é obrigatório e ocupa a primeira posição.
- As abas não ativas permanecem montadas; filtros, paginação, modais e contexto dos componentes continuam vivos durante a sessão.
- O estado de abas, aba ativa, recentes e favoritos é serializado em `localStorage` como `familyplus-workspace-v1` e restaurado na reabertura.
- A command palette é global (`Ctrl + K`), aceita código ou texto e reutiliza o mesmo gerenciador. `Alt + 1` a `Alt + 9` foca abas abertas.
- O fechamento de abas passa pelo shell; o Dashboard não fecha. A interface está preparada para acoplar sinalização de alterações pendentes e fluxos de salvar/descartar quando cada formulário passar a expor seu estado de edição.

## Organização

| Camada | Responsabilidade |
|---|---|
| Controllers | Contrato REST, sessão e envelope HTTP |
| Services | Regras puras/orquestração de domínio e validações |
| Data/Entities | Mapeamento EF Core e esquema SQLite |
| DTOs | Contratos de entrada e saída |
| Migrations | Evolução versionada do esquema |
| Front-end | Fluxos de cadastro, movimentação, cartão, recorrência, previsão, assinatura e planejamento mensal |

`CardService` coordena cartões, compras, parcelas, faturas, estornos, limite e projeções. `TransactionService` mantém o saldo e o resumo financeiro coerentes com a origem da transação. `RecurrenceService` gerencia regras, ocorrências, exceções, efetivação, assinaturas e a integração entre planejamento, conta e cartão. `DashboardService` concentra agregações gerenciais sem gravar totais derivados; `AlertService` concentra geração idempotente, leitura e resolução dos alertas.
