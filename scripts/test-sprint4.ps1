param([string]$BaseUrl = 'http://localhost:5162')

$ErrorActionPreference = 'Stop'

function Assert-Equal([string]$Name, $Expected, $Actual) {
  if ($Expected -ne $Actual) { throw "FALHOU $Name. Esperado: $Expected. Atual: $Actual." }
  Write-Output "PASSOU $Name ($Actual)"
}

function Invoke-FamilyApi([string]$Method, [string]$Path, $Body = $null, $Headers = @{}) {
  $params = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $Headers; ContentType = 'application/json'; UseBasicParsing = $true }
  if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress) }
  $response = Invoke-WebRequest @params
  $envelope = $response.Content | ConvertFrom-Json
  if (-not $envelope.success) { throw "API recusou $Method ${Path}: $($envelope.errors -join ' ')" }
  return $envelope.data
}

$setup = Invoke-FamilyApi POST '/api/auth/setup' @{ nome = 'Teste Sprint 4'; login = 'sprint4'; senha = 'senha-local-123' }
$headers = @{ 'X-Session-Token' = $setup.token }
$member = Invoke-FamilyApi POST '/api/membros' @{ nome = 'Família de teste'; descricao = 'Aceite Sprint 4' } $headers
$account = Invoke-FamilyApi POST '/api/contas' @{ membroId = $member.id; nome = 'Conta de teste'; tipo = 'ContaCorrente'; saldoInicialCentavos = 500000; observacao = 'Aceite' } $headers
$incomeCategory = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Salário'; tipo = 'Receita'; categoriaPaiId = $null } $headers
$expenseCategory = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Moradia'; tipo = 'Despesa'; categoriaPaiId = $null } $headers
$card = Invoke-FamilyApi POST '/api/cartoes' @{ membroId = $member.id; nome = 'Cartão para assinatura'; bandeira = 'Visa'; ultimosDigitos = '4444'; limiteTotalCentavos = 300000; diaFechamento = 25; diaVencimento = 5; contaPagamentoPadraoId = $account.id; observacao = $null } $headers

$today = [datetimeoffset]::UtcNow.Date.AddHours(12)
$recurrence = Invoke-FamilyApi POST '/api/recorrencias' @{ membroId = $member.id; contaId = $account.id; categoriaId = $expenseCategory.id; tipo = 'DESPESA'; descricao = 'Aluguel'; valorCentavos = 125000; frequencia = 'MENSAL'; dataInicio = $today.ToString('o'); dataFim = $null; diaReferencia = $today.Day; gerarAutomaticamente = $true; valorVariavel = $false; classificacao = 'CONTA_FIXA'; observacao = 'Aceite' } $headers
$occurrences = Invoke-FamilyApi GET "/api/ocorrencias-recorrentes?recorrenciaId=$($recurrence.id)" $null $headers
if (@($occurrences).Count -lt 12) { throw 'FALHOU geração de horizonte: esperadas ao menos 12 ocorrências.' }
$first = @($occurrences | Where-Object { $_.status -eq 'PENDENTE' })[0]
$balance = Invoke-FamilyApi GET "/api/contas/$($account.id)/saldo" $null $headers
Assert-Equal 'previsão não altera saldo' 500000 $balance.saldoAtualCentavos
Invoke-FamilyApi POST "/api/ocorrencias-recorrentes/$($first.id)/efetivar" @{ valorRealizadoCentavos = 130000; dataEfetivacao = $today.ToString('o'); observacao = 'Valor real' } $headers | Out-Null
$balance = Invoke-FamilyApi GET "/api/contas/$($account.id)/saldo" $null $headers
Assert-Equal 'efetivação cria despesa real' 370000 $balance.saldoAtualCentavos

$updated = Invoke-FamilyApi PUT "/api/recorrencias/$($recurrence.id)" @{ membroId = $member.id; contaId = $account.id; categoriaId = $expenseCategory.id; tipo = 'DESPESA'; descricao = 'Aluguel atualizado'; valorCentavos = 140000; frequencia = 'MENSAL'; dataInicio = $today.ToString('o'); dataFim = $null; diaReferencia = $today.Day; gerarAutomaticamente = $true; valorVariavel = $true; classificacao = 'CONTA_FIXA'; observacao = 'Atualizado' } $headers
$afterUpdate = Invoke-FamilyApi GET "/api/ocorrencias-recorrentes?recorrenciaId=$($recurrence.id)" $null $headers
Assert-Equal 'histórico efetivado preservado' 130000 (@($afterUpdate | Where-Object { $_.id -eq $first.id })[0].valorRealizadoCentavos)
Assert-Equal 'próximas previsões recalculadas' 140000 (@($afterUpdate | Where-Object { $_.status -eq 'PENDENTE' })[0].valorPrevistoCentavos)

$subscription = Invoke-FamilyApi POST '/api/assinaturas' @{ membroId = $member.id; nome = 'Streaming anual'; categoriaId = $expenseCategory.id; contaId = $account.id; valorCentavos = 60000; periodicidade = 'ANUAL'; dataInicio = $today.ToString('o'); proximaCobranca = $today.ToString('o'); metodoPagamento = 'CARTAO'; cartaoId = $card.id; observacao = $null } $headers
Assert-Equal 'assinatura anual normaliza custo mensal' 5000 $subscription.custoMensalEquivalenteCentavos
$purchases = Invoke-FamilyApi GET "/api/compras-cartao?cartaoId=$($card.id)" $null $headers
Assert-Equal 'assinatura no cartão gera compra' 1 @($purchases).Count
Invoke-FamilyApi PATCH "/api/assinaturas/$($subscription.id)/status" $false $headers | Out-Null
$subscriptions = Invoke-FamilyApi GET '/api/assinaturas' $null $headers
Assert-Equal 'cancelamento preserva registro da assinatura' $false (@($subscriptions | Where-Object { $_.id -eq $subscription.id })[0].ativa)

Write-Output 'Sprint 4: todos os testes de aceitação passaram.'
