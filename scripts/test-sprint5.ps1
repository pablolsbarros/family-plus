param([string]$BaseUrl = 'http://localhost:5162')

$ErrorActionPreference = 'Stop'

function Assert-Equal([string]$Name, $Expected, $Actual) {
  if ($Expected -ne $Actual) { throw "FALHOU $Name. Esperado: $Expected. Atual: $Actual." }
  Write-Output "PASSOU $Name ($Actual)"
}

function Invoke-FamilyApi([string]$Method, [string]$Path, $Body = $null, $Headers = @{}) {
  $params = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $Headers; ContentType = 'application/json'; UseBasicParsing = $true }
  if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress) }
  $response = Invoke-WebRequest @params
  $envelope = $response.Content | ConvertFrom-Json
  if (-not $envelope.success) { throw "API recusou $Method ${Path}: $($envelope.errors -join ' ')" }
  return $envelope.data
}

$setup = Invoke-FamilyApi POST '/api/auth/setup' @{ nome = 'Teste Sprint 5'; login = 'sprint5'; senha = 'senha-local-123' }
$headers = @{ 'X-Session-Token' = $setup.token }
$member = Invoke-FamilyApi POST '/api/membros' @{ nome = 'Família de teste'; descricao = 'Aceite Sprint 5' } $headers
$account = Invoke-FamilyApi POST '/api/contas' @{ membroId = $member.id; nome = 'Conta principal'; tipo = 'ContaCorrente'; saldoInicialCentavos = 500000; observacao = 'Aceite' } $headers
$incomeCategory = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Salário'; tipo = 'Receita'; categoriaPaiId = $null } $headers
$expenseCategory = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Casa'; tipo = 'Despesa'; categoriaPaiId = $null } $headers
$card = Invoke-FamilyApi POST '/api/cartoes' @{ membroId = $member.id; nome = 'Cartão teste'; bandeira = 'Visa'; ultimosDigitos = '5555'; limiteTotalCentavos = 500000; diaFechamento = 5; diaVencimento = 5; contaPagamentoPadraoId = $account.id; observacao = $null } $headers
$today = [datetimeoffset]::UtcNow.Date.AddHours(12)
$todayIso = $today.ToString('o')

Invoke-FamilyApi POST '/api/transacoes' @{ tipo = 'RECEITA'; descricao = 'Salário'; valorCentavos = 1000000; dataCompetencia = $todayIso; dataMovimentacao = $todayIso; contaId = $account.id; categoriaId = $incomeCategory.id; membroId = $member.id; status = 'EFETIVADA'; observacao = $null } $headers | Out-Null
Invoke-FamilyApi POST '/api/transacoes' @{ tipo = 'DESPESA'; descricao = 'Mercado'; valorCentavos = 700000; dataCompetencia = $todayIso; dataMovimentacao = $todayIso; contaId = $account.id; categoriaId = $expenseCategory.id; membroId = $member.id; status = 'EFETIVADA'; observacao = $null } $headers | Out-Null
Invoke-FamilyApi POST '/api/transacoes' @{ tipo = 'DESPESA'; descricao = 'Conta futura'; valorCentavos = 100000; dataCompetencia = $today.AddDays(8).ToString('o'); dataMovimentacao = $today.AddDays(8).ToString('o'); contaId = $account.id; categoriaId = $expenseCategory.id; membroId = $member.id; status = 'PREVISTA'; observacao = $null } $headers | Out-Null
Invoke-FamilyApi POST '/api/recorrencias' @{ membroId = $member.id; contaId = $account.id; categoriaId = $expenseCategory.id; tipo = 'DESPESA'; descricao = 'Energia recorrente'; valorCentavos = 50000; frequencia = 'MENSAL'; dataInicio = $today.AddDays(10).ToString('o'); dataFim = $null; diaReferencia = $today.AddDays(10).Day; gerarAutomaticamente = $true; valorVariavel = $false; classificacao = 'NORMAL'; observacao = $null } $headers | Out-Null
Invoke-FamilyApi POST '/api/compras-cartao' @{ cartaoId = $card.id; membroId = $member.id; categoriaId = $expenseCategory.id; descricao = 'Compra cartão'; valorTotalCentavos = 200000; quantidadeParcelas = 1; dataCompra = $todayIso; observacao = $null } $headers | Out-Null

$summary = Invoke-FamilyApi GET '/api/dashboard/resumo?horizonteDias=30' $null $headers
Assert-Equal 'receitas do período' 1000000 $summary.periodo.receitasCentavos
Assert-Equal 'despesas do período incluem cartão sem duplicar pagamento' 900000 $summary.periodo.despesasCentavos
Assert-Equal 'resultado financeiro' 100000 $summary.periodo.resultadoCentavos
Assert-Equal 'saldo atual consolidado' 800000 $summary.saldo.saldoConsolidadoCentavos
Assert-Equal 'despesas previstas no horizonte' 150000 $summary.projecao.despesasPrevistasCentavos
Assert-Equal 'fatura futura na projeção' 200000 $summary.projecao.faturasPrevistasCentavos
Assert-Equal 'saldo projetado' 450000 $summary.projecao.saldoProjetadoCentavos
Assert-Equal 'taxa de poupança' 10 $summary.kpis.taxaPoupancaPercentual
if (@($summary.despesasCategorias).Count -eq 0) { throw 'FALHOU despesas por categoria: retorno vazio.' }
Write-Output 'PASSOU despesas por categoria'

Invoke-FamilyApi POST '/api/transacoes' @{ tipo = 'DESPESA'; descricao = 'Estouro controlado'; valorCentavos = 1000000; dataCompetencia = $todayIso; dataMovimentacao = $todayIso; contaId = $account.id; categoriaId = $expenseCategory.id; membroId = $member.id; status = 'EFETIVADA'; observacao = $null } $headers | Out-Null
$alertsFirst = Invoke-FamilyApi GET '/api/alertas?resolvido=false' $null $headers
$alertsSecond = Invoke-FamilyApi GET '/api/alertas?resolvido=false' $null $headers
$balanceAlerts = @($alertsSecond | Where-Object { $_.tipo -eq 'SALDO_NEGATIVO' })
Assert-Equal 'alerta de saldo negativo sem duplicidade' 1 $balanceAlerts.Count
Assert-Equal 'quantidade de alertas estável' @($alertsFirst).Count @($alertsSecond).Count
Invoke-FamilyApi POST "/api/alertas/$($balanceAlerts[0].id)/resolver" $null $headers | Out-Null
$remaining = Invoke-FamilyApi GET '/api/alertas?resolvido=false' $null $headers
if (@($remaining | Where-Object { $_.id -eq $balanceAlerts[0].id }).Count -ne 0) { throw 'FALHOU resolução do alerta.' }
Write-Output 'PASSOU resolução de alerta'
Write-Output 'Sprint 5: todos os testes de aceitação passaram.'
