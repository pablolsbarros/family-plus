# Setup e validação limpa — Sprint 6

## Executar

Para iniciar a API e a interface em duas janelas do PowerShell automaticamente, execute `iniciar-familyplus.bat` na raiz do repositório. Pelo PowerShell, o equivalente é:

```powershell
.\scripts\run-local.ps1
```

Para instalar ou atualizar as dependências do front-end antes da inicialização:

```powershell
.\scripts\run-local.ps1 -InstallDependencies
```

O inicializador usa a API em `http://localhost:5160` e a interface em `http://localhost:5173`.

Para executar manualmente, em dois terminais:

```powershell
dotnet restore .\FamilyPlus.sln
Set-Location .\frontend
npm install

# Em um terminal na raiz
dotnet run --project .\backend\src\FamilyPlus.Api --urls http://localhost:5160

# Em outro terminal
Set-Location .\frontend
npm run dev
```

Abra `http://localhost:5173`. A API cria ou migra o SQLite automaticamente.

## Aceite manual de cartões

1. Cadastre membro, conta bancária e categoria de despesa.
2. Cadastre um cartão com limite, fechamento, vencimento e conta padrão.
3. Registre compra à vista: confirme que o limite cai, mas o saldo da conta não muda.
4. Registre uma compra parcelada cujo valor não divida exatamente pelas parcelas e confira se a última contém o resíduo.
5. Compare compras antes/depois do dia de fechamento e confirme a competência correta.
6. Abra os detalhes da fatura, feche-a e pague integralmente pela conta selecionada.
7. Confira no extrato o débito de pagamento e no resumo que o consumo não foi contado duas vezes.
8. Cancele uma compra de fatura aberta; para compra fechada/paga, envie estorno por `POST /api/compras-cartao/{id}/estornar` e confirme o total recalculado.

## Aceite manual de recorrências

1. Cadastre membro, conta e categorias de receita/despesa.
2. Em **Recorrências**, crie uma despesa mensal com geração automática e confirme que as previsões dos próximos 12 meses aparecem.
3. Confirme que o saldo da conta não muda apenas pela previsão; efetive uma ocorrência com valor diferente e confirme a criação da movimentação real no extrato.
4. Edite a regra e confira que somente previsões futuras são recalculadas, preservando a ocorrência já efetivada.
5. Cadastre uma conta fixa e use o item específico do menu para filtrá-la.
6. Cadastre uma assinatura anual. Confira o custo mensal equivalente e anual; escolha conta para gerar despesa prevista ou cartão para gerar compra na fatura.
7. Cancele a assinatura e confirme que ela continua visível como inativa, sem apagar o histórico.

## Aceite manual do Dashboard

1. Cadastre uma receita efetivada, uma despesa efetivada, uma transferência e uma compra no cartão.
2. Abra **Dashboard > Visão geral** e confira saldo atual, receitas, despesas e resultado. A transferência não deve alterar o resultado e o pagamento da fatura não deve duplicar o consumo.
3. Troque o horizonte entre 7, 15, 30, 60 e 90 dias e confirme a atualização do saldo projetado, fluxo de caixa e próximos vencimentos.
4. Selecione um membro específico e confirme que contas, transações, cartões e categorias respeitam o filtro.
5. Abra **Dashboard > Fluxo de caixa**, **Alertas** e **Indicadores-chave (KPIs)**.
6. Crie uma situação de saldo negativo, fatura próxima ou limite de cartão acima de 70%. Confirme o alerta, marque-o como lido e resolva-o. Atualizar a tela não deve criar duplicatas.

## Aceite manual do planejamento

1. Cadastre uma categoria pai de despesa (por exemplo, Alimentação) e ao menos uma filha (Mercado). Em **Planejamento > Orçamento mensal**, crie o orçamento do mês com receita prevista.
2. Adicione um limite para o pai e registre despesas efetivadas na categoria filha. Confira realizado, disponível, percentual usado e que a soma das filhas aparece no pai.
3. Tente adicionar um limite para pai e filha no mesmo ramo. A API deve rejeitar a operação para evitar dupla contagem.
4. Registre uma compra no cartão. Confira que ela entra uma vez no realizado pelo mês da compra; pagar a fatura não deve somar novamente.
5. Registre uma transferência entre contas e confira que ela não muda o realizado do orçamento.
6. Registre uma despesa prevista ou recorrência futura no mesmo mês. Confira `Projetado` e o alerta de estouro projetado quando realizado mais previsto superar o limite.
7. Alcance 70%, 90% e 100% de uso e confira os alertas de atenção, crítico e estourado na central.
8. Use **Copiar para outro mês**, informe um reajuste opcional, confira que os itens foram copiados sem realizado/alertas e encerre o mês. O orçamento encerrado deve continuar no histórico.

## Validações automatizadas

```powershell
dotnet build .\FamilyPlus.sln
dotnet test .\backend\tests\FamilyPlus.Api.Tests\FamilyPlus.Api.Tests.csproj
Set-Location .\frontend; npm run build
```

Para testar a API sem tocar na base principal, inicie uma instância isolada:

```powershell
$env:FAMILYPLUS_STORAGE_ROOT = 'C:\tmp\FamilyPlusSprint4Acceptance'
dotnet run --project .\backend\src\FamilyPlus.Api --urls http://localhost:5162
```

Em outro terminal:

```powershell
.\scripts\test-sprint3.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint4.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint5.ps1 -BaseUrl http://localhost:5162
.\scripts\test-sprint6.ps1 -BaseUrl http://localhost:5162
```

Consulte também `http://localhost:5160/swagger` e `http://localhost:5160/health`.
