---
description: Exibe referência rápida do LMA v1.0 — princípios, estrutura, templates, anti-padrões
argument-hint: [principios|estrutura|templates|antipatterns|all]
---

# /lma-docs — Referência rápida LMA

## Argumento

- `principios` (default): regras invioláveis
- `estrutura`: pastas + nomenclatura
- `templates`: os 5 templates de Ação
- `antipatterns`: os "nunca faça"
- `all`: documento completo

## Fontes

| Seção | Fonte |
|---|---|
| `principios` | Inline abaixo |
| `estrutura`, `templates`, `antipatterns` | `${CLAUDE_PLUGIN_ROOT}/docs/lma-templates.md` e `lma-v1.0.md` |
| `all` | `${CLAUDE_PLUGIN_ROOT}/docs/lma-v1.0.md` completo |

## Conteúdo inline: `principios`

```
═══════════════════════════════════════════════════════════════
  Lean Modular Architecture (LMA) v1.0 — Princípios Invioláveis
  "Arquitetura modular para APIs .NET que pensam em produtividade
   humana e de IA"
═══════════════════════════════════════════════════════════════

DOIS PROJETOS FÍSICOS (compilador valida):
  MinhaApp.Domain → puro, zero infra (sem EF, MediatR, ASP.NET)
  MinhaApp.Api    → web + infra, referencia Domain

ESTRUTURA DE UMA AÇÃO (pasta plana):
  Modulos/{Modulo}/{Acao}/
  ├── {Acao}.cs              ← nome = verbo de negócio (sem sufixo!)
  ├── {Acao}Request.cs
  ├── {Acao}Response.cs
  └── {Acao}Validator.cs

ROTEAMENTO CENTRALIZADO:
  Endpoints/{Modulo}Endpoints.cs  ← TODA rota do módulo aqui

CONVENÇÕES FIXAS:
  - Método público único: Execute(request, ct)
  - Retorno: Result<TResponse> sempre
  - Acesso a dados: AppDbContext ou IDbConnectionFactory direto (sem Repository)
  - Validação: automática via IEndpointFilter (Ação assume Request válido)
  - Commit: SaveChangesAsync(ct) explícito na última linha de escrita
  - JWT/Claims: extraídos no Endpoint, passados limpos no Request

STACK:
  ✓ .NET 8+, Minimal API, EF Core e/ou Dapper, FluentValidation
  ✗ MediatR, AutoMapper, Controllers MVC, IRepository<T>
  ✗ Sufixo Handler/Service/UseCase/Reader/Writer/Command/Query

OS 7 "NUNCA FAÇA":
  1. Ação injeta outra Ação
  2. Domain importa framework de infra
  3. Ação conhece HttpContext/ClaimsPrincipal
  4. Criar Repository ou Writer
  5. Ação declara própria rota
  6. Validação manual dentro da Ação
  7. Escrita sem SaveChangesAsync

TEMPLATES DE AÇÃO:
  A — Leitura EF Core (filtros, paginação)
  B — Leitura Dapper (relatório, SQL complexo)
  C — Escrita CRUD (anêmico, sem regras)
  D — Escrita com Regra (entidade rica, vocabulário ubíquo)
  E — Escrita com Gateway (pagamento, e-mail crítico, terceiro)

═══════════════════════════════════════════════════════════════
  /lma-docs estrutura | templates | antipatterns | all
  Skills: lma-create-action | lma-add-module | lma-refactor-to-rich
═══════════════════════════════════════════════════════════════
```

## Output para seções longas

Após exibir a seção, ofereça:

```
📌 Próximos passos:
   - Criar Ação:   skill `lma-create-action`
   - Criar módulo: skill `lma-add-module`
   - Refatorar:    skill `lma-refactor-to-rich`
   - Revisar PR:   /lma-review [--staged | --last-commit | <caminho>]
```
