# ADR-0001 — Perfil dedicado de saúde financeira

## Contexto

O Dashboard precisa guardar metas e a seleção familiar de categorias essenciais. Esses parâmetros variam por família e podem ter uma sobrescrita opcional por membro. Os indicadores continuam sendo derivados dos dados operacionais locais.

## Decisão

Persistir metas, observação e seleção de categorias essenciais na tabela dedicada `perfil_saude_financeira`, com escopo familiar e `membro_id` opcional. Manter agregações e fórmulas calculadas sob demanda pelo serviço de saúde financeira, integradas ao Dashboard. Não classificar categorias globalmente como essenciais.

## Consequências

- O banco local recebe uma migration e um novo perfil por família e por membro.
- Alterações em lançamentos, recorrências, orçamento e filtros são refletidas na próxima consulta sem armazenar totais derivados.
- A seleção de categorias é armazenada como IDs JSON associados ao perfil; cada ID é validado como categoria de despesa ativa da família ao salvar.
- Indicadores sem denominador ou histórico suficiente retornam valor nulo acompanhado de estado, para evitar exibir zero indevido.

## Alternativas consideradas

- Reutilizar `configuracao`: rejeitada por não representar dados relacionais com escopo familiar e de membro.
- Marcar categorias como essenciais globalmente: rejeitada porque a decisão pertence ao perfil de cada família.

## Referências

- `docs/business-rules.md`
- Ticket local `FAM-19`
