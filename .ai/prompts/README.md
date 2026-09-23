# Prompts operacionais

Esta pasta concentra instruções reutilizáveis para as etapas locais do `ai-dev`, como planejamento, implementação e revisão.

Regras:

1. `AGENTS.md` é a regra operacional principal e o Jira é a fonte funcional de cada trabalho.
2. Prompts não podem conter tokens, senhas, dados reais de família ou comandos destrutivos.
3. Todo prompt deve pedir critérios de aceite verificáveis e informar os comandos de validação definidos em `.ai/pipeline.json`.
4. Mudanças de arquitetura, banco, autenticação ou contrato REST exigem registro em `docs/decisions/`.

