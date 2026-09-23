param([string]$BaseUrl = 'http://localhost:5161')
$ErrorActionPreference = 'Stop'
$apiBase = "$BaseUrl/api"

function Invoke-Api([string]$Method, [string]$Path, $Body = $null, [string]$Token = '') {
  $headers = @{}; if ($Token) { $headers['X-Session-Token'] = $Token }
  $params = @{ Method = $Method; Uri = "$apiBase$Path"; Headers = $headers; UseBasicParsing = $true }
  if ($null -ne $Body) { $params['ContentType'] = 'application/json'; $params['Body'] = ($Body | ConvertTo-Json -Depth 10) }
  $response = Invoke-WebRequest @params; $envelope = $response.Content | ConvertFrom-Json
  if (-not $envelope.success) { throw "API recusou $Method $Path`: $($envelope.message) $($envelope.errors -join ' ')" }
  return $envelope.data
}
function Assert-Equal($Actual, $Expected, [string]$Name) {
  if ($Actual -ne $Expected) { throw "${Name}: esperado $Expected, obtido $Actual" }
  Write-Host "PASS $Name ($Actual)" -ForegroundColor Green
}

$setup = Invoke-Api 'POST' '/auth/setup' @{ nome = 'Teste Sprint 2'; login = 'sprint2'; senha = 'senha-local-123' }; $token = $setup.token
$member = Invoke-Api 'POST' '/membros' @{ nome = 'Família de teste'; descricao = 'Aceitação Sprint 2' } $token
$accountA = Invoke-Api 'POST' '/contas' @{ membroId = $member.id; nome = 'Conta origem'; tipo = 'ContaCorrente'; saldoInicialCentavos = 100000; observacao = $null } $token
$accountB = Invoke-Api 'POST' '/contas' @{ membroId = $member.id; nome = 'Conta destino'; tipo = 'ContaDigital'; saldoInicialCentavos = 100000; observacao = $null } $token
$incomeCategory = Invoke-Api 'POST' '/categorias' @{ nome = 'Salário teste'; tipo = 'Receita'; categoriaPaiId = $null } $token
$expenseCategory = Invoke-Api 'POST' '/categorias' @{ nome = 'Mercado teste'; tipo = 'Despesa'; categoriaPaiId = $null } $token
$date = (Get-Date).ToUniversalTime().ToString('o')
$common = @{ dataCompetencia = $date; dataMovimentacao = $date; contaId = $accountA.id; membroId = $member.id; observacao = $null }
Invoke-Api 'POST' '/receitas' ($common + @{ tipo = 'RECEITA'; descricao = 'Receita cenário 1'; valorCentavos = 200000; categoriaId = $incomeCategory.id; status = 'EFETIVADA' }) $token | Out-Null
Invoke-Api 'POST' '/despesas' ($common + @{ tipo = 'DESPESA'; descricao = 'Despesa cenário 1'; valorCentavos = 50000; categoriaId = $expenseCategory.id; status = 'EFETIVADA' }) $token | Out-Null
$balance = Invoke-Api 'GET' "/contas/$($accountA.id)/saldo" $null $token; Assert-Equal $balance.saldoAtualCentavos 250000 'saldo inicial + receita - despesa'
$planned = Invoke-Api 'POST' '/despesas' ($common + @{ tipo = 'DESPESA'; descricao = 'Despesa prevista'; valorCentavos = 30000; categoriaId = $expenseCategory.id; status = 'PREVISTA' }) $token
$balance = Invoke-Api 'GET' "/contas/$($accountA.id)/saldo" $null $token; Assert-Equal $balance.saldoAtualCentavos 250000 'despesa prevista não altera saldo'
$planned = Invoke-Api 'PUT' "/transacoes/$($planned.id)" ($common + @{ tipo = 'DESPESA'; descricao = 'Despesa prevista efetivada'; valorCentavos = 30000; categoriaId = $expenseCategory.id; status = 'EFETIVADA' }) $token
$balance = Invoke-Api 'GET' "/contas/$($accountA.id)/saldo" $null $token; Assert-Equal $balance.saldoAtualCentavos 220000 'prevista -> efetivada reduz saldo'
$transfer = Invoke-Api 'POST' '/transferencias' @{ contaOrigemId = $accountA.id; contaDestinoId = $accountB.id; membroId = $member.id; valorCentavos = 50000; data = $date; descricao = 'Transferência cenário 2'; status = 'EFETIVADA'; observacao = $null } $token
$balanceA = Invoke-Api 'GET' "/contas/$($accountA.id)/saldo" $null $token; $balanceB = Invoke-Api 'GET' "/contas/$($accountB.id)/saldo" $null $token
Assert-Equal $balanceA.saldoAtualCentavos 170000 'saldo da origem após transferência'; Assert-Equal $balanceB.saldoAtualCentavos 150000 'saldo do destino após transferência'; Assert-Equal ($balanceA.saldoAtualCentavos + $balanceB.saldoAtualCentavos) 320000 'patrimônio total inalterado pela transferência'
$invalid = $false; try { Invoke-Api 'POST' '/despesas' ($common + @{ tipo = 'DESPESA'; descricao = 'Categoria inválida'; valorCentavos = 100; categoriaId = $incomeCategory.id; status = 'EFETIVADA' }) $token | Out-Null } catch { $invalid = $true }; if (-not $invalid) { throw 'Categoria de receita foi aceita em uma despesa.' }; Write-Host 'PASS categoria incompatível bloqueada' -ForegroundColor Green
$filtered = Invoke-Api 'GET' "/transacoes?tipo=DESPESA&contaId=$($accountA.id)&pagina=1&tamanhoPagina=10" $null $token; if ($filtered.totalItens -lt 2) { throw "Filtro de despesas retornou $($filtered.totalItens) itens." }; Write-Host "PASS filtro de transações ($($filtered.totalItens) itens)" -ForegroundColor Green
$statement = Invoke-Api 'GET' "/contas/$($accountA.id)/extrato" $null $token; if ($statement.movimentacoes.Count -lt 4) { throw 'Extrato não contém os movimentos esperados.' }; Write-Host "PASS extrato ($($statement.movimentacoes.Count) movimentos)" -ForegroundColor Green
Invoke-Api 'PATCH' "/transferencias/$($transfer.id)/cancelar" $null $token | Out-Null
$balanceA = Invoke-Api 'GET' "/contas/$($accountA.id)/saldo" $null $token; $balanceB = Invoke-Api 'GET' "/contas/$($accountB.id)/saldo" $null $token
Assert-Equal $balanceA.saldoAtualCentavos 220000 'cancelamento atômico restaura origem'; Assert-Equal $balanceB.saldoAtualCentavos 100000 'cancelamento atômico restaura destino'
Write-Host 'Sprint 2: todos os testes de aceitação passaram.' -ForegroundColor Cyan
