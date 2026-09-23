param([string]$BaseUrl = 'http://localhost:5162')

$ErrorActionPreference = 'Stop'

function Assert-Equal([string]$Name, $Expected, $Actual) {
  if ($Expected -ne $Actual) { throw "FALHOU $Name. Esperado: $Expected. Atual: $Actual." }
  Write-Output "PASSOU $Name ($Actual)"
}

function Invoke-FamilyApi([string]$Method, [string]$Path, $Body = $null, $Headers = @{}) {
  $params = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $Headers; ContentType = 'application/json'; UseBasicParsing = $true }
  if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress) }
  $envelope = (Invoke-WebRequest @params).Content | ConvertFrom-Json
  if (-not $envelope.success) { throw "API recusou $Method ${Path}: $($envelope.errors -join ' ')" }
  return $envelope.data
}

$suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
$setup = Invoke-FamilyApi POST '/api/auth/setup' @{ nome = "Teste Sprint 6"; login = "sprint6$suffix"; senha = 'senha-local-123' }
$headers = @{ 'X-Session-Token' = $setup.token }
$member = Invoke-FamilyApi POST '/api/membros' @{ nome = 'Família de teste'; descricao = 'Aceite Sprint 6' } $headers
$account = Invoke-FamilyApi POST '/api/contas' @{ membroId = $member.id; nome = 'Conta principal'; tipo = 'ContaCorrente'; saldoInicialCentavos = 500000; observacao = 'Aceite' } $headers
$parent = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Alimentação'; tipo = 'Despesa'; categoriaPaiId = $null } $headers
$child = Invoke-FamilyApi POST '/api/categorias' @{ nome = 'Mercado'; tipo = 'Despesa'; categoriaPaiId = $parent.id } $headers
$today = [datetimeoffset]::UtcNow.Date.AddHours(12)
$todayIso = $today.ToString('o')

$budget = Invoke-FamilyApi POST '/api/orcamentos' @{ ano = $today.Year; mes = $today.Month; membroId = $null; descricao = 'Orçamento de teste'; receitaPrevistaCentavos = 1000000; modoReceitaPrevista = 'MANUAL'; status = 'RASCUNHO'; observacao = $null } $headers
Invoke-FamilyApi POST "/api/orcamentos/$($budget.id)/itens" @{ categoriaId = $parent.id; membroId = $null; valorPlanejadoCentavos = 150000; observacao = $null } $headers | Out-Null
Invoke-FamilyApi POST '/api/transacoes' @{ tipo = 'DESPESA'; descricao = 'Mercado'; valorCentavos = 90000; dataCompetencia = $todayIso; dataMovimentacao = $todayIso; contaId = $account.id; categoriaId = $child.id; membroId = $member.id; status = 'EFETIVADA'; observacao = $null } $headers | Out-Null

$summary = Invoke-FamilyApi GET "/api/orcamentos/$($budget.id)/resumo" $null $headers
$line = @($summary.linhas | Where-Object { $_.categoriaId -eq $parent.id })[0]
Assert-Equal 'total planejado' 150000 $summary.totalPlanejadoCentavos
Assert-Equal 'categoria pai soma filha' 90000 $line.realizadoCentavos
Assert-Equal 'disponível' 60000 $line.disponivelCentavos
Assert-Equal 'percentual utilizado' 60 $line.percentualUtilizado

$projection = Invoke-FamilyApi GET "/api/orcamentos/$($budget.id)/projecao" $null $headers
Assert-Equal 'projeção sem previsão adicional' 90000 $projection.totalProjetadoCentavos
Invoke-FamilyApi POST "/api/orcamentos/$($budget.id)/ativar" $null $headers | Out-Null
$dashboard = Invoke-FamilyApi GET '/api/dashboard/orcamento' $null $headers
Assert-Equal 'dashboard integrado' $budget.id $dashboard.orcamentoId
Invoke-FamilyApi POST "/api/orcamentos/$($budget.id)/encerrar" $null $headers | Out-Null
$history = Invoke-FamilyApi GET "/api/orcamentos?ano=$($today.Year)&mes=$($today.Month)" $null $headers
Assert-Equal 'histórico preservado' 1 @($history).Count
Write-Output 'Sprint 6: todos os testes de aceitação básicos passaram.'
