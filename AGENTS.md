# AI Development Rules — Family+

Este arquivo é a constituição operacional das IAs que trabalham no Family+.

## Fonte de verdade

A ordem de precedência é:

1. Jira e critérios de aceite do ticket.
2. `docs/business-rules.md`.
3. `docs/architecture.md`.
4. Contratos de API em `backend/src/FamilyPlus.Api/DTOs` e controladores REST.
5. Código existente.

Em caso de conflito, não invente comportamento. Interrompa a implementação, descreva a divergência e solicite uma decisão humana.

## Git

Nunca:

- trabalhar diretamente em `main`;
- trabalhar diretamente em `develop`;
- executar merge;
- executar force-push;
- apagar branches remotas;
- criar Pull Request ou enviar alterações sem autorização explícita.

Quando existir uma base remota versionada, use worktrees e branches no padrão `ai/FAM-<numero>`.

## Segurança

Nunca:

- exibir secrets, tokens, senhas ou PINs;
- modificar arquivos `.env`, credenciais ou banco de dados real sem autorização explícita;
- versionar `.env.ai-dev`;
- executar `DROP`, `TRUNCATE` ou apagar dados financeiros;
- modificar infraestrutura de produção;
- introduzir uma dependência de negócio remota em um produto local/offline sem autorização explícita.

Valores financeiros são sempre centavos inteiros. Não use `float`.

## Implementação

Antes de concluir uma alteração:

- executar build;
- executar os testes aplicáveis;
- verificar lint;
- documentar decisões e limitações relevantes;
- executar os comandos aplicáveis de `.ai/pipeline.json`.

O arquivo `.ai/pipeline.json` é controlado por humanos e não pode ser alterado para fazer verificações passarem.

## Alterações arquiteturais

Mudanças em:

- contratos públicos;
- banco de dados;
- autenticação;
- autorização;
- arquitetura;

devem ser explicitamente apontadas no resultado e, quando relevantes, registradas em `docs/decisions/`.

## Revisão

A IA que implementou uma alteração não deve ser a única responsável pela aprovação técnica.
