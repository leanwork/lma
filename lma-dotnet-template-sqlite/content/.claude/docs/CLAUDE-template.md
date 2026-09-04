# CLAUDE.md

> Projeto seguindo **Lean Modular Architecture (LMA) v1.0** — arquitetura modular para APIs .NET otimizada para produtividade humana e de IA.
>
> Documento completo: `docs/architecture/lma-v1.0.md`

## Regras invioláveis

**Dois projetos físicos (compilador valida):**
1. `MinhaApp.Domain` — entidades ricas, Value Objects, Domain Services. **Zero dependências de infra** (sem EF, sem ASP.NET, sem FluentValidation, sem MediatR).
2. `MinhaApp.Api` — Endpoints, Ações, Infrastructure. Referencia `Domain`.

**Estrutura de cada Ação** (pasta plana, sem subpastas):
```
Modulos/{Modulo}/{NomeAcao}/
├── {NomeAcao}.cs              ← classe de Ação (sempre)
├── {NomeAcao}Request.cs       ← se há input
├── {NomeAcao}Response.cs      ← se há output estruturado
└── {NomeAcao}Validator.cs     ← se há entrada validável
```

**Roteamento centralizado por módulo:**
```
Endpoints/{Modulo}Endpoints.cs   ← extension method que mapeia TODAS as rotas do módulo
```

**Convenções fixas (não inventar variação):**
- Nome da classe = verbo de negócio (`ProcessarPedido`, `BuscarProdutos`). Sem sufixo `Handler`/`Service`/`UseCase`.
- Método público único: **`Execute(request, ct)`**
- Retorno: **`Result<TResponse>`** sempre
- Validação: automática via `IEndpointFilter` no grupo (Ação assume Request válido)
- Commit: **explícito** — `await db.SaveChangesAsync(ct)` na última linha transacional da Ação

## Stack

**Obrigatório:** .NET 8+, Minimal API, EF Core e/ou Dapper, FluentValidation, xUnit + FluentAssertions + Testcontainers.
**Proibido:** MediatR, AutoMapper, Controllers MVC, `IRepository<T>`, Repository/Writer por agregado, DTOs em pasta central, sufixo `Handler`/`Service`/`UseCase` nas Ações.

## Os 7 "nunca faça" críticos

1. ❌ Ação **nunca** injeta outra Ação — consulta o banco direto ou usa Domain Service.
2. ❌ Domain **nunca** importa `Microsoft.EntityFrameworkCore`, `MediatR`, `Microsoft.AspNetCore.*` ou qualquer framework de infra.
3. ❌ Ação **nunca** vê `HttpContext`, `ClaimsPrincipal` ou nada de protocolo web — Endpoint extrai e passa limpo no Request.
4. ❌ **Nunca** criar Repository, Writer, `IPedidoRepository`, `IPedidoWriter` etc. — LMA acessa `DbContext`/Dapper direto na Ação.
5. ❌ Ação **nunca** declara a própria rota — roteamento mora em `Endpoints/{Modulo}Endpoints.cs`.
6. ❌ Ação **nunca** valida formato manualmente — `IEndpointFilter` faz isso antes.
7. ❌ Ação de escrita **nunca** retorna sem `SaveChangesAsync(ct)` — commit é explícito e responsabilidade da Ação.

Lista completa de anti-padrões: `docs/architecture/lma-v1.0.md` §17.

## Quando criar uma Ação nova

Use a skill **`lma-create-action`** (ativa automaticamente em "criar ação", "novo endpoint", "criar caso de uso", "adicionar funcionalidade").

Sem a skill, siga manualmente: `docs/architecture/lma-templates.md`.

## Quando criar um módulo novo

Use a skill **`lma-add-module`** (ativa em "criar módulo", "adicionar bounded context", "criar área de X").

## Quando revisar código

Use o checklist: `docs/architecture/lma-checklist-pr.md`.
Ou invoque o subagent **`lma-reviewer`** para análise profunda.

## Quando NÃO seguir LMA

Pare e discuta com o arquiteto se: precisa trocar persistência **por agregado dentro do mesmo módulo**, domínio tem complexidade combinatória extrema (motor de seguros/tributação), múltiplos runtimes compartilhando lógica de aplicação, ou ambiente regulado com necessidade de rastreabilidade rigorosa.

Detalhes: `docs/architecture/lma-v1.0.md` §18.
