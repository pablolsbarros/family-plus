# Sprint 8 — Core Ledger

O Family+ v0.8 passa a tratar `lancamento` como a fonte canônica dos fatos financeiros. `transacao` permanece como espelho de compatibilidade das APIs das Sprints anteriores e possui `LancamentoId` para rastreabilidade durante a transição.

## Regras do domínio

- Todo lançamento possui tipo, natureza, status, origem, membro, conta e data de competência.
- Valores monetários são inteiros em centavos; ajustes podem ser positivos ou negativos.
- O saldo é derivado do saldo inicial e dos lançamentos `EFETIVADO`. Lançamentos `PREVISTO` participam somente da projeção.
- Consumo de cartão é registrado no Ledger, mas não reduz a conta bancária. O pagamento de fatura reduz a conta com natureza `PAGAMENTO_FATURA` e não volta a compor despesas.
- Transferências geram dois lançamentos atômicos, com natureza `TRANSFERENCIA`; não entram em receita ou despesa.
- Previsões recorrentes usam a ocorrência como chave de idempotência. A efetivação promove o lançamento previsto e preserva `ValorPrevistoCentavos`.
- Cancelamento/estorno altera status ou cria ajuste; fatos financeiros não são removidos fisicamente.

## Endpoints canônicos

```text
GET/POST/PUT /api/lancamentos
POST /api/lancamentos/{id}/efetivar
POST /api/lancamentos/{id}/cancelar
POST /api/lancamentos/ajustes

GET /api/contas/{id}/saldo
GET /api/contas/{id}/saldo?data=2026-09-30
GET /api/contas/{id}/extrato
GET /api/contas/{id}/projecao

GET/POST/PUT /api/transferencias
GET/POST /api/compras-cartao
GET /api/faturas
POST /api/faturas/{id}/fechar
POST /api/faturas/{id}/pagar
POST /api/recorrencias/processar
POST /api/ocorrencias-recorrentes/{id}/efetivar
```

Todas as rotas protegidas respeitam a sessão local, a família atual e as permissões de movimentação, transferência, cartão e recorrência. Escopos `PROPRIOS`, `FAMILIA` e `SELECIONADOS` são aplicados nas consultas do Ledger quando configurados para o usuário.

## Migração

`20260914013650_Sprint8CoreLedger` cria a tabela canônica, adiciona vínculos de lançamento a transações, faturas e ocorrências, e converte registros legados sem recadastro. A aplicação continua local/offline e executa `Database.MigrateAsync()` na inicialização.

## Validação executada

- Suíte backend: 28 testes aprovados.
- Build do backend: aprovado sem warnings.
- Aplicação das migrations em SQLite temporário: aprovada.
- Casos cobertos: saldo derivado, transferência sem dupla contagem, compra/pagamento de cartão, arredondamento existente, recorrência idempotente e ajustes.
