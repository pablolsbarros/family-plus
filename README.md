# Family+ — Sistema Financeiro Local

O Family+ é uma aplicação financeira familiar **local e offline**. A versão **v0.6** reúne a fundação local, movimentações bancárias, cartões, recorrências, planejamento mensal e Dashboard gerencial: saldos, receitas, despesas, fluxo de caixa, projeções, cartões, KPIs, categorias, filtros, alertas e auditoria em SQLite.

> Nenhuma regra financeira depende de serviços externos. Todos os valores são armazenados em centavos inteiros.

## Stack

- Front-end: React 19, TypeScript e Vite
- Back-end: ASP.NET Core 9, C# e REST
- Persistência: SQLite + Entity Framework Core
- Documentação interativa: Swagger UI
- Segurança local: senha derivada com PBKDF2-SHA512 e tokens de sessão armazenados somente como hash

## Executar localmente

Para abrir a API e a interface automaticamente em duas janelas do PowerShell, dê duplo clique em [`iniciar-familyplus.bat`](iniciar-familyplus.bat) na raiz do repositório.

Se preferir executar pelo PowerShell, use:

```powershell
.\scripts\run-local.ps1
```

Para instalar ou atualizar as dependências do front-end antes de iniciar:

```powershell
.\scripts\run-local.ps1 -InstallDependencies
```

O script abre duas janelas persistentes:

- API em `http://localhost:5160`
- Interface em `http://localhost:5173`

Também é possível iniciar manualmente, em dois terminais na raiz do repositório:

```powershell
# Terminal 1 — API em http://localhost:5160
dotnet run --project .\backend\src\FamilyPlus.Api --urls http://localhost:5160
```

```powershell
# Terminal 2 — interface em http://localhost:5173
Set-Location .\frontend
npm install
npm run dev
```

Abra `http://localhost:5173`. O SQLite é criado ou migrado automaticamente em `backend/src/FamilyPlus.Api/data/familyplus.db`.

## Sprint 3 — cartões e faturas

- Cartão vinculado a membro, com bandeira, final, limite, dias de fechamento/vencimento e conta padrão de pagamento.
- Compra à vista ou parcelada. O rateio preserva o valor total, colocando eventuais centavos residuais na última parcela.
- Regra de competência centralizada: compra até o dia de fechamento entra no mês; depois dele, no mês seguinte.
- Compras reduzem o limite disponível, mas não o saldo bancário. O débito na conta ocorre somente ao pagar a fatura.
- Faturas abertas, fechadas, vencidas, pagas ou canceladas; fechamento automático na inicialização e fechamento manual pela interface/API.
- O pagamento integral gera uma despesa técnica auditável, que é excluída do resumo para não contar o consumo duas vezes.
- Cancelamentos antes do fechamento e estornos (inclusive parciais) preservam histórico e recalculam as faturas.

## Sprint 4 — recorrências, contas fixas e assinaturas

- Receitas e despesas recorrentes com frequência diária, semanal, quinzenal, mensal, bimestral, trimestral, semestral ou anual.
- Geração automática de previsões pelo horizonte de 12 meses, com unicidade por recorrência e data planejada.
- Previsões pendentes, atrasadas, efetivadas, ignoradas ou canceladas. Previsão não muda saldo; efetivação cria a transação efetivada correspondente.
- Edição de uma recorrência recalcula somente previsões futuras: ocorrências efetivadas e seu histórico são preservados.
- Contas fixas são recorrências de despesa identificadas para filtro e acompanhamento próprios.
- Assinaturas com cobrança por conta (despesa prevista) ou cartão (compra de uma parcela na fatura), custo mensal/anual normalizado e cancelamento sem apagar histórico.
- A área **Recorrências** do workspace possui visão geral, receitas, despesas, contas fixas, próximos vencimentos, assinaturas e custo total de assinaturas.

## Workspace de navegação

O Family+ funciona como um workspace local: o menu lateral encontra funcionalidades e as abas mantêm o trabalho ativo em paralelo. Cada tela possui código funcional permanente, rota, módulo, descrição, permissão conceitual e política de instância única.

- A mesma tela não é duplicada na sessão: ela é apenas trazida para a aba ativa.
- O Dashboard (`DSH001`) permanece como primeira aba e não pode ser fechado.
- Use **Ctrl + K** ou **Ir para tela** para pesquisar por código ou nome, por exemplo `FIN001` ou `orçamento`.
- Favoritos, telas recentes, abas abertas e última aba ativa são preservados localmente neste navegador/dispositivo.
- `Alt + 1` até `Alt + 9` alterna entre as abas abertas correspondentes.

As áreas cuja regra de negócio ainda será entregue apresentam um hub de módulo explícito. Elas já participam da navegação, da busca, dos favoritos e da preservação da sessão, sem simular integrações ou dados que ainda não existem.

## Sprint 5 — Dashboard e indicadores

- Dashboard conectado aos dados reais, com saldo consolidado e detalhamento por conta.
- Receitas, despesas e resultado por período, sem contar transferências ou pagamento de fatura como consumo duplicado.
- Saldo projetado configurável em 7, 15, 30, 60 ou 90 dias, somando previsões de contas/recorrências e descontando faturas futuras.
- Fluxo de caixa com entradas e saídas realizadas, previstas, faturas e saldo acumulado.
- Próximas receitas, próximas despesas, faturas, limite utilizado, despesas por categoria e evolução mensal.
- KPIs de taxa de poupança, comprometimento da renda e gastos fixos.
- Central de alertas para vencimentos, saldo negativo, projeção negativa, recorrências atrasadas e limite de cartão alto; alertas são persistidos e não duplicados.
- Filtros de período e membro aplicados na visão gerencial.

## API e Swagger

- Swagger UI: `http://localhost:5160/swagger`
- Documento OpenAPI: `http://localhost:5160/swagger/v1/swagger.json`
- Health check: `http://localhost:5160/health`

Todas as rotas de domínio exigem `X-Session-Token`, recebido no setup ou no login. A interface web o administra automaticamente.

## Sprint 6 — planejamento financeiro

- Orçamentos mensais por família ou membro, com receita prevista manual, status rascunho/ativo/encerrado e histórico preservado.
- Itens de orçamento por categoria e membro, com unicidade por orçamento/categoria/membro e validação para não misturar categoria pai e filha no mesmo ramo.
- Realizado derivado automaticamente de despesas efetivadas e compras de cartão pela competência de consumo; transferências e pagamentos de fatura ficam fora para evitar dupla contagem.
- Resumo com planejado, realizado, disponível, percentual usado, variação, margem planejada, poupança planejada e projeção de despesas futuras conhecidas.
- Categorias sem orçamento são exibidas e alertadas; faixas saudável, atenção, crítico e estourado alimentam a central de alertas.
- Cópia de orçamento para outro mês com reajuste percentual opcional, encerramento/reabertura e integração do orçamento ativo ao Dashboard.
- APIs em `/api/orcamentos` e `/api/dashboard/orcamento`; a migration `20260914000206_Sprint6Planejamento` cria `orcamento` e `item_orcamento` com índices operacionais.

## Sprint 7 — fundação e modelo de acesso

- Família, membro e usuário são entidades separadas; sessões carregam contexto de família, perfil e permissões.
- Perfis `ADMINISTRADOR`, `MEMBRO` e `SOMENTE_LEITURA`, escopos de dados e isolamento por `familia_id` no backend.
- Administração de membros, usuários locais, convites offline, perfil financeiro por membro e rateio de despesas sem duplicação.
- Configurações de família, moeda, locale, timezone, preferências, notificações e segurança local.
- Backup ZIP versionado com manifesto, restauração segura, integridade SQLite e estrutura preparada para CSV/OFX/Open Finance.
- Migration `20260914010140_Sprint7AccessModel` e bootstrap de dados legados para a família padrão sem recadastro.

Consulte [Sprint 7](docs/sprint7.md) para rotas, contratos, migração, validações e limites intencionais da v0.7.

Consulte [setup](docs/setup.md), [arquitetura](docs/architecture.md), [banco](docs/database.md) e [API](docs/api.md) para os contratos e regras completas.

## Validação

```powershell
dotnet build .\FamilyPlus.sln
dotnet test .\backend\tests\FamilyPlus.Api.Tests\FamilyPlus.Api.Tests.csproj
Set-Location .\frontend; npm run build

# Com uma API isolada em http://localhost:5162:
.\scripts\test-sprint3.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint4.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint5.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint6.ps1 -BaseUrl http://localhost:5162
```
