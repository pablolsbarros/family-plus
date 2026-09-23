param(
    [Parameter(Position=0)]
    [string]$Command,

    [Parameter(Position=1)]
    [string]$Argument
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

& "$ScriptDir\.venv\Scripts\python.exe" `
    "$ScriptDir\ai_dev.py" `
    $Command `
    $Argument

exit $LASTEXITCODE
