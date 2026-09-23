param(
    [switch]$InstallDependencies
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$BackendProject = Join-Path $Root 'backend\src\FamilyPlus.Api'
$Frontend = Join-Path $Root 'frontend'

if (-not (Test-Path -LiteralPath $BackendProject -PathType Container)) {
    throw "Projeto da API não encontrado: $BackendProject"
}

if (-not (Test-Path -LiteralPath $Frontend -PathType Container)) {
    throw "Diretório do front-end não encontrado: $Frontend"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'O comando dotnet não foi encontrado no PATH.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'O comando npm não foi encontrado no PATH.'
}

if ($InstallDependencies) {
    Push-Location $Frontend
    try {
        npm install
    }
    finally {
        Pop-Location
    }
}

$shell = Get-Command pwsh -ErrorAction SilentlyContinue
if (-not $shell) {
    $shell = Get-Command powershell.exe -ErrorAction SilentlyContinue
}

if (-not $shell) {
    throw 'Nenhum PowerShell foi encontrado no PATH.'
}

$apiCommand = "`$Host.UI.RawUI.WindowTitle = 'Family+ API'; Write-Host 'API: http://localhost:5160' -ForegroundColor Cyan; dotnet run --project .\backend\src\FamilyPlus.Api --urls http://localhost:5160"
$frontendCommand = "`$Host.UI.RawUI.WindowTitle = 'Family+ Interface'; Write-Host 'Interface: http://localhost:5173' -ForegroundColor Cyan; npm run dev"

Start-Process -FilePath $shell.Source `
    -WorkingDirectory $Root `
    -ArgumentList @('-NoExit', '-ExecutionPolicy', 'Bypass', '-Command', $apiCommand) `
    -WindowStyle Normal | Out-Null

Start-Process -FilePath $shell.Source `
    -WorkingDirectory $Frontend `
    -ArgumentList @('-NoExit', '-ExecutionPolicy', 'Bypass', '-Command', $frontendCommand) `
    -WindowStyle Normal | Out-Null

Write-Host 'Family+ iniciado.' -ForegroundColor Green
Write-Host 'API:       http://localhost:5160'
Write-Host 'Interface: http://localhost:5173'
Write-Host 'Feche as duas janelas abertas para encerrar os serviços.' -ForegroundColor DarkGray
