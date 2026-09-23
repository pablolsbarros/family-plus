# Regras de negócio — Family+

Este documento reúne as invariantes de negócio que devem ser preservadas em toda alteração. O ticket do Jira e seus critérios de aceite complementam estas regras; em caso de conflito, a implementação deve ser interrompida para decisão humana.

## Princípios financeiros

1. Valores monetários usam centavos inteiros. Não usar `float` ou arredondamentos implícitos.
2. O sistema é local e offline. Nenhuma integração remota pode se tornar requisito de negócio sem autorização explícita.
3. Exclusões físicas não são usadas para dados financeiros: cancelar, arquivar ou estornar deve manter histórico e auditoria.
4. Datas e horários são tratados em UTC. Competências mensais devem preservar o mês esperado no fuso da família.
5. Totais derivados são calculados a partir de fatos operacionais; não devem ser duplicados como saldo editável.

## Movimentações e contas

1. O saldo de uma conta decorre somente de transações efetivadas.
2. Transferências relacionam origem e destino e não podem alterar o resultado consolidado da família.
3. Categorias, contas e membros arquivados não podem ser usados em novos lançamentos.
4. Toda alteração financeira relevante deve produzir auditoria local.

## Cartões e faturas

1. Compra no cartão reduz limite disponível, mas não altera saldo bancário.
2. O pagamento da fatura altera a conta bancária, porém não é uma nova despesa de consumo.
3. A competência da compra é definida pelo ciclo de fechamento do cartão.
4. Parcelamentos devem preservar exatamente o valor total, inclusive os centavos residuais.
5. Cancelamentos e estornos preservam o histórico e recalculam a fatura relacionada.

## Recorrências e planejamento

1. Recorrências geram previsões; elas não modificam o saldo até serem efetivadas.
2. A geração de ocorrências deve ser idempotente e não duplicar a mesma recorrência na mesma data prevista.
3. Alterar uma recorrência afeta somente previsões futuras pendentes ou atrasadas; o histórico efetivado é preservado.
4. Orçamento compara o planejado com despesas realizadas por competência e pode considerar previsões somente em projeções futuras.
5. Transferências e pagamentos técnicos de fatura ficam fora do consumo para evitar dupla contagem.

## Família, segurança e privacidade

1. Os dados devem ser isolados por família e respeitar o perfil/permissões do usuário local.
2. Tokens, senhas, PINs e arquivos `.env` nunca podem ser incluídos em código, logs, documentação ou commits.
3. Operações de backup e restauração devem validar a integridade antes de substituir dados locais.

