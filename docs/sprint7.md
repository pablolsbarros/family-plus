# Sprint 7 — Fundação e modelo de acesso (v0.7)

## Entregas implementadas

- `familia` como agrupador do espaço financeiro, com moeda padrão e timezone.
- Separação definitiva entre `usuario`, `membro` e `familia`; membro pode existir sem login.
- `familia_id` nas entidades financeiras e filtro de contexto aplicado pelo `FinanceDbContext`.
- Provisionamento compatível com instalações antigas: primeira família `Minha Família`, membro titular do usuário existente, catálogo de perfis/permissões e preferências padrão.
- Perfis `ADMINISTRADOR`, `MEMBRO` e `SOMENTE_LEITURA`, catálogo de permissões e escopos `PROPRIOS`, `FAMILIA` e `SELECIONADOS`.
- Administração de família, membros, usuários locais, perfis, permissões e perfil financeiro consolidado.
- Rateio de despesas por percentual, valor fixo e divisão igualitária. O serviço fecha o total em centavos e nunca cria uma segunda transação.
- Configuração de família, preferências individuais, notificações, segurança de senha e bloqueio da sessão.
- Conexões financeiras preparadas para `MANUAL`, `OFX`, `CSV` e `OPEN_FINANCE`, sem conexão externa nesta versão.
- Backup ZIP versionado com `manifest.json`, banco SQLite e diretório de documentos locais quando existente; restauração valida o manifesto e cria cópia de segurança antes de substituir o banco.
- `DatabaseHealthService` materializado no endpoint de sistema com schema aplicado e `PRAGMA integrity_check`.
- Auditoria ampliada para login/logout, administração de membros/usuários/permissões, configurações, backup e restauração.

## Rotas principais

| Área | Rotas |
|---|---|
| Família | `GET/PUT /api/familia`, `GET /api/familia/perfil-financeiro` |
| Membros | `GET/POST /api/membros`, `PUT /api/membros/{id}`, `PATCH /api/membros/{id}/status` |
| Usuários | `GET/POST /api/usuarios`, `PATCH /api/usuarios/{id}/status`, `GET/PUT /api/usuarios/{id}/permissoes` |
| Rateio | `GET/POST/PUT/DELETE /api/transacoes/{id}/rateio` |
| Configurações | `/api/configuracoes/familia`, `/preferencias`, `/notificacoes`, `/sistema` |
| Segurança | `POST /api/seguranca/alterar-senha`, `/bloquear`, `/desbloquear` |
| Backup | `POST /api/configuracoes/backup`, `GET /backup/historico`, `POST /backup/restaurar` |

## Migração e compatibilidade

A Migration 007 (`Sprint7AccessModel`) cria a família, acesso, configuração, rateio, importação e conexão financeira. Colunas de contexto são adicionadas de forma compatível e o bootstrap da aplicação associa os registros existentes à família padrão sem exigir recadastro.

O filtro global é desativado apenas no bootstrap/diagnóstico controlado (`IgnoreQueryFilters`); nas requisições autenticadas o `RequireSessionFilter` injeta o `familia_id` da sessão no contexto do banco. Perfis somente leitura são bloqueados no backend para métodos de escrita, independentemente da interface.

## Validações executadas

- `dotnet build FamilyPlus.sln --no-restore` — sucesso, sem avisos.
- `dotnet test FamilyPlus.sln --no-restore` — 24 testes aprovados.
- `npm run build` em `frontend` — TypeScript e Vite aprovados.
- E2E em storage temporário: boot do schema 7, integridade SQLite OK, configuração `America/Cuiaba`, backup ZIP com manifesto v0.7, rateio 60/40 fechando 100.000 centavos, rateio inválido HTTP 400 e escrita por somente leitura HTTP 403.

## Limites intencionais da v0.7

Open Finance não abre conexão real, convites continuam sendo acessos locais e a troca do arquivo SQLite na restauração pede reinício da aplicação para que todas as conexões recarreguem o banco restaurado.
