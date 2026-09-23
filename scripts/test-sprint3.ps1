param([string]$BaseUrl = 'http://localhost:5162')

$ErrorActionPreference = 'Stop'

function Assert-Equal([string]$Name, $Expected, $Actual) {
  if ($Expected -ne $Actual) { throw "FAIL $Name. Esperado: $Expected. Atual: $Actual." }
  Write-Output "PASS $Name ($Actual)"
}

function Invoke-FamilyApi([string]$Method, [string]$Path, $Body = $null, $Headers = @{}) {
  $params = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $Headers; ContentType = 'application/json'; UseBasicParsing = $true }
  if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 8 -Compress) }
  $response = Invoke-WebRequest @params
  $envelope = $response.Content | ConvertFrom-Json
  if (-not $envelope.success) { throw "API recusou $Method ${Path}: $($envelope.errors -join ' ')" }
  return $envelope.data
}

$setup = Invoke-FamilyApi POST '/api/auth/setup' @{ nome = 'Teste Sprint 3'; login = 'sprint3'; senha = 'senha-local-123' }
$headers = @{ 'X-Session-Token' = $setup.token }
$member = Invoke-FamilyApi POST '/api/membros' @{ nome = 'Família de teste'; descricao = 'Aceite Sprint 3' } $headers
$account = Invoke-FamilyApi POST '/api/contas' @{ membroId = $member.id; nome = 'Conta pagamento'; tipo = 'ContaCorrente'; saldoInicialCentavos = 500000; observacao = 'Aceite' } $headers
$category = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Compras no cartão'; tipo = 'Despesa'; categoriaPaiId = $null } $headers
$card = Invoke-FamilyApi POST '/api/cartoes' @{ membroId = $member.id; nome = 'Cartão de teste'; bandeira = 'Visa'; ultimosDigitos = '1234'; limiteTotalCentavos = 500000; diaFechamento = 5; diaVencimento = 12; contaPagamentoPadraoId = $account.id; observacao = 'Aceite' } $headers

$simple = Invoke-FamilyApi POST '/api/compras-cartao' @{ cartaoId = $card.id; membroId = $member.id; categoriaId = $category.id; descricao = 'Compra simples'; valorTotalCentavos = 100000; quantidadeParcelas = 1; dataCompra = '2026-09-04T12:00:00Z'; observacao = $null } $headers
$limit = Invoke-FamilyApi GET "/api/cartoes/$($card.id)/limite" $null $headers
Assert-Equal 'compra simples consome limite' 100000 $limit.limiteUtilizadoCentavos
Assert-Equal 'compra simples preserva limite disponível' 400000 $limit.limiteDisponivelCentavos
$balance = Invoke-FamilyApi GET "/api/contas/$($account.id)/saldo" $null $headers
Assert-Equal 'compra no cartão não altera saldo bancário' 500000 $balance.saldoAtualCentavos

$installment = Invoke-FamilyApi POST '/api/compras-cartao' @{ cartaoId = $card.id; membroId = $member.id; categoriaId = $category.id; descricao = 'Compra parcelada'; valorTotalCentavos = 120000; quantidadeParcelas = 6; dataCompra = '2026-09-04T12:00:00Z'; observacao = $null } $headers
Assert-Equal 'compra parcelada gera seis parcelas' 6 @($installment.parcelas).Count
Assert-Equal 'parcelas preservam valor total' 120000 (@($installment.parcelas | Measure-Object -Property valorCentavos -Sum).Sum)
Assert-Equal 'primeira parcela na competência correta' 9 ([datetimeoffset]$installment.parcelas[0].dataCompetencia).Month
Assert-Equal 'segunda parcela projetada para o mês seguinte' 10 ([datetimeoffset]$installment.parcelas[1].dataCompetencia).Month

$invoices = Invoke-FamilyApi GET "/api/faturas?cartaoId=$($card.id)" $null $headers
$september = @($invoices | Where-Object { ([datetimeoffset]$_.competencia).Month -eq 9 })[0]
Assert-Equal 'fatura de setembro soma compra e primeira parcela' 120000 $september.valorTotalCentavos
Invoke-FamilyApi PATCH "/api/faturas/$($september.id)/fechar" $null $headers | Out-Null
Invoke-FamilyApi POST "/api/faturas/$($september.id)/pagar" @{ contaPagamentoId = $account.id; dataPagamento = '2026-09-12T12:00:00Z'; valorCentavos = 120000 } $headers | Out-Null
$paid = Invoke-FamilyApi GET "/api/faturas/$($september.id)" $null $headers
Assert-Equal 'pagamento marca fatura como paga' 'PAGA' $paid.fatura.status
$balance = Invoke-FamilyApi GET "/api/contas/$($account.id)/saldo" $null $headers
Assert-Equal 'pagamento reduz saldo bancário' 380000 $balance.saldoAtualCentavos
$summary = Invoke-FamilyApi GET '/api/movimentacoes/resumo?dataInicio=2026-09-01&dataFim=2026-09-30' $null $headers
Assert-Equal 'pagamento não duplica despesa de consumo' 220000 $summary.despesasCentavos
$limit = Invoke-FamilyApi GET "/api/cartoes/$($card.id)/limite" $null $headers
Assert-Equal 'pagamento libera limite das parcelas quitadas' 100000 $limit.limiteUtilizadoCentavos

Write-Output 'Sprint 3: todos os testes de aceitação passaram.'
