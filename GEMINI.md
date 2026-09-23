# Instruções para Gemini — Family+

Leia `AGENTS.md` antes de analisar ou alterar o projeto.

## Contexto do produto

- O Family+ é uma aplicação financeira familiar local e offline.
- A fonte funcional de trabalho é o Jira, projeto `FAM`, com requisitos e critérios de aceite no ticket.
- O projeto usa React/Vite no frontend e ASP.NET Core com SQLite no backend.

## Limites obrigatórios

1. Não expor, ler em voz alta, alterar ou versionar arquivos `.env`, tokens, PINs, senhas ou bancos reais.
2. Não alterar diretamente `main` ou `develop`; não fazer push, merge ou Pull Request sem autorização humana explícita.
3. Não criar dependências remotas, serviços financeiros externos ou operações destrutivas no SQLite sem autorização.
4. Usar centavos inteiros para valores financeiros.
5. Registrar decisões de arquitetura em `docs/decisions/` e executar as validações de `.ai/pipeline.json` quando aplicáveis.

