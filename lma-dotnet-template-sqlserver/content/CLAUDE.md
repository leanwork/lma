# CLAUDE.md

> Projeto seguindo **Lean Modular Architecture (LMA) v1.0** — arquitetura modular para APIs .NET otimizada para produtividade humana e de IA.
>
> Documento completo: `docs/architecture/lma-v1.0.md`

## Regras invioláveis

**Dois projetos físicos (compilador valida):**
1. `LmaApp.Domain` — entidades ricas, Value Objects, Domain Services. **Zero dependências de infra** (sem EF, sem ASP.NET, sem FluentValidation, sem MediatR).
2. `LmaApp.Api` — Endpoints, Ações, Infrastructure. Referencia `Domain`.

**Estrutura de cada Ação** (pasta plana, sem subpastas):
```
Modulos/{Modulo}/{NomeAcao}/
├── {NomeAcao}.cs              ← classe de Ação (sempre)
├── {NomeAcao}Models.cs        ← Request, Response e Validator (ou arquivos separados)
```

**Roteamento centralizado por módulo:**
```
Endpoints/{Modulo}Endpoints.cs   ← TODAS as rotas do módulo aqui
```

**Convenções fixas:**
- Nome da classe = verbo de negócio (`ProcessarPedido`, `BuscarProdutos`). Sem sufixo `Handler`/`Service`/`UseCase`.
- Método público único: **`Execute(request, ct)`**
- Retorno: **`Result<TResponse>`** sempre
- Validação: automática via `IEndpointFilter` no grupo (Ação assume Request válido)
- Commit: **explícito** — `await db.SaveChangesAsync(ct)` na última linha transacional

## Stack

**Obrigatório:** .NET 8+, Minimal API, EF Core e/ou Dapper, FluentValidation, xUnit + FluentAssertions + Testcontainers.
**Proibido:** MediatR, AutoMapper, Controllers MVC, `IRepository<T>`, Repository/Writer por agregado, DTOs em pasta central.

## Os 7 "nunca faça"

1. ❌ Ação **nunca** injeta outra Ação
2. ❌ Domain **nunca** importa framework de infra
3. ❌ Ação **nunca** vê `HttpContext` ou `ClaimsPrincipal`
4. ❌ **Nunca** criar Repository ou Writer
5. ❌ Ação **nunca** declara a própria rota
6. ❌ Ação **nunca** valida formato manualmente
7. ❌ Ação de escrita **nunca** retorna sem `SaveChangesAsync(ct)`

## Referências

- `docs/architecture/lma-v1.0.md` — documento completo
- `docs/architecture/lma-templates.md` — 5 templates de Ação
- `docs/architecture/lma-checklist-pr.md` — checklist de PR
