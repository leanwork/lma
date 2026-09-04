# LmaApp

API .NET seguindo **Lean Modular Architecture (LMA) v1.0** — Leanwork Group.

Banco de dados: **PostgreSQL**.

## Estrutura

```
src/
├── LmaApp.Domain/     # Entidades ricas, Value Objects, Domain Services (zero infra)
└── LmaApp.Api/        # Endpoints, Ações, Infrastructure

tests/
└── LmaApp.Tests/      # Testes de integração (EF InMemory, sem Docker)

.claude/               # Skills, commands, agent e hooks LMA para o Claude Code
docs/architecture/     # Documento LMA v1.0, templates de Ação e checklist de PR
```

## Rodando localmente

```bash
# 1. Subir PostgreSQL e a stack de observabilidade
docker compose up -d

# 2. Restaurar e rodar
dotnet restore
dotnet run --project src/LmaApp.Api
```

A migration é aplicada automaticamente em `Development`. Ajuste a connection string em
`src/LmaApp.Api/appsettings.Development.json`.

## Observabilidade

| Ferramenta | URL | O que faz |
|---|---|---|
| Swagger | `http://localhost:{porta}/swagger` | Documentação da API |
| Jaeger | `http://localhost:16686` | Traces distribuídos (OpenTelemetry) |
| Seq | `http://localhost:5341` | Logs estruturados (só com `--UseSeq true`) |

## Rodando os testes

```bash
dotnet test
```

Os testes usam EF Core InMemory — **não precisam de Docker**. Para rodar contra um PostgreSQL
real, o pacote `Testcontainers.PostgreSql` já está referenciado: descomente o container em
`tests/LmaApp.Tests/ApiFactory.cs`.

## Criando migration

```bash
dotnet ef migrations add <NomeDaMigration> --project src/LmaApp.Api
dotnet ef database update --project src/LmaApp.Api
```

## Claude Code

Este projeto já vem com a pasta `.claude/` configurada — skills, commands, subagent e hooks são
carregados automaticamente ao abrir o Claude Code na raiz do projeto. Nada a instalar.

```
/lma-docs          # referência rápida da arquitetura
/lma-review        # revisar mudanças contra o checklist de PR
```

Skills ativam sozinhas pelo contexto da conversa:

- `lma-create-action` — "criar ação para listar clientes"
- `lma-add-module` — "criar módulo de Checkout"
- `lma-refactor-to-rich` — "tornar Pedido uma entidade rica"

## Documentação

- `docs/architecture/lma-v1.0.md` — Referência completa
- `docs/architecture/lma-templates.md` — 5 templates de Ação
- `docs/architecture/lma-checklist-pr.md` — Checklist de PR
- `CLAUDE.md` — Guia para IA (Claude Code, Cursor, Copilot)
