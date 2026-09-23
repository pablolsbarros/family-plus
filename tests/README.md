# Testes do Family+

Os testes automatizados atuais da API estão em `backend/tests/FamilyPlus.Api.Tests`, porque fazem parte da solução .NET.

Esta pasta raiz é reservada para verificações de integração entre componentes, cenários ponta a ponta, dados de teste compartilhados e documentação de qualidade que não pertençam exclusivamente ao backend ou ao frontend.

Antes de concluir uma tarefa, execute o pipeline controlado por humanos:

```powershell
.\tools\ai-dev\ai-dev.ps1 test
```

