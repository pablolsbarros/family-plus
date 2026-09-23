# AI-DEV — Family+

Orquestrador linear do fluxo Jira → Google → Codex → pipeline → revisão → PR.

## Uso

Dentro do repositório:

```powershell
.\tools\ai-dev\ai-dev.ps1 run FAM-123
```

Depois de adicionar `C:\FamilyPlus\tools\ai-dev` ao `PATH` do usuário, o
mesmo comando pode ser chamado como:

```powershell
ai-dev run FAM-123
```

Para testar somente na sessão atual do PowerShell:

```powershell
$env:Path = "C:\FamilyPlus\tools\ai-dev;$env:Path"
ai-dev status
```

## Comandos

```text
doctor
plan <TICKET>
run <TICKET>
review <TICKET>
test
test <TICKET>
status [TICKET]
cleanup <TICKET>
```

## Proteções

O fluxo completo exige, no mínimo:

```text
AI_DEV_ALLOW_EXTERNAL=true
AI_DEV_ALLOW_PUSH=true
AI_DEV_ALLOW_PR=true
AI_DEV_ALLOW_CLEANUP=true
```

Sem essas autorizações, o orquestrador não acessa agentes externos, não faz
push, não cria Pull Requests nem remove worktrees. O ticket é trabalhado em um
worktree baseado em `origin/develop`.
