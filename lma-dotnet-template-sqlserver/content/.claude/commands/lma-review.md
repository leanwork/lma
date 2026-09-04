---
description: Roda checklist de revisão LMA nos arquivos alterados (git diff ou caminho específico), apontando violações dos princípios e anti-padrões
argument-hint: [caminho|--staged|--last-commit]
---

# /lma-review — Revisar código LMA

Aplica o checklist de PR LMA v1.0 sobre arquivos alterados ou caminho específico.

## Argumento

- **Vazio** ou `--staged`: revisa arquivos staged
- `--last-commit`: revisa arquivos do último commit
- `<caminho>`: revisa arquivo ou pasta específica

## Comportamento

### 1. Identificar arquivos

```bash
# staged
git diff --cached --name-only --diff-filter=AM | grep "\.cs$"
# last-commit
git show --name-only --diff-filter=AM --pretty=format: HEAD | grep "\.cs$"
# caminho
find <caminho> -name "*.cs"
```

### 2. Classificar cada arquivo por componente LMA

| Path/Nome | Componente |
|---|---|
| `MinhaApp.Domain/**/*.cs` | Domain |
| `Endpoints/*Endpoints.cs` | Roteamento |
| `Modulos/*/*.cs` (sem sufixo técnico) | Ação |
| `Modulos/*/*Request.cs` | Request DTO |
| `Modulos/*/*Response.cs` | Response DTO |
| `Modulos/*/*Validator.cs` | Validator |
| `Infrastructure/Database/*.cs` | Infra/BD |
| `Infrastructure/Gateways/*.cs` | Gateway |
| `Common/ValidationFilter.cs` | Filtro global |

### 3. Verificações por componente

**Domain:**
- 🔥 `using Microsoft.EntityFrameworkCore` → bloqueante
- 🔥 `using MediatR` → bloqueante
- 🔥 `using Microsoft.AspNetCore.*` → bloqueante
- ⚠️ Setter público em entidade com método de comportamento
- ⚠️ Coleção `List<T>` pública mutável (deveria ser `IReadOnlyList<T>`)

**Ação:**
- 🔥 Nome de classe termina com `Handler`, `Service`, `UseCase`, `Reader`, `Writer`, `Command`, `Query`
- 🔥 Injeta `IHttpContextAccessor`, `ClaimsPrincipal`, `HttpContext`
- 🔥 Injeta outra Ação (`new ClassName` ou no construtor)
- 🔥 Injeta `IRepository<*>`, `I*Repository`, `I*Writer`
- 🔥 Contém `app.MapGet/MapPost/MapPut/MapDelete` (rota dentro da Ação)
- 🔥 Usa `SaveChangesAsync` mas nunca faz `SaveChangesAsync` (esqueceu)
- ⚠️ `Execute` não existe como método público
- ⚠️ Não retorna `Result<T>` ou `Result`
- ⚠️ Regra de `if (entidade.Status == ...)` dentro da Ação (indício de regra fora do Domain)
- ⚠️ `using Microsoft.AspNetCore.*` (protocolo web vazando)

**Leitura:**
- 🔥 `SaveChangesAsync` em Ação de leitura
- ⚠️ Sem `AsNoTracking()` em query EF

**Escrita:**
- 🔥 Sem `SaveChangesAsync` mas modifica estado

**Endpoints:**
- ⚠️ Não aplica `.AddEndpointFilter<ValidationFilter>()`
- ⚠️ Extrai dados de JWT/Claims dentro da Ação em vez do Endpoint

**Validators:**
- ⚠️ Falta validator para Request com campos não-triviais

**MediatR / AutoMapper:**
- 🔥 `using MediatR` em qualquer arquivo
- 🔥 `using AutoMapper` em qualquer arquivo
- 🔥 Classe implementando `IRequest<>` ou `IRequestHandler<>`

### 4. Formato de saída

```
🔍 Revisão LMA v1.0 — {N} arquivos analisados

🔥 {X} violações bloqueantes
⚠️  {Y} avisos

─────────────────────────────────────────────────────
📄 MinhaApp.Api/Modulos/Checkout/CancelarPedido/CancelarPedido.cs

🔥 Regra de negócio na Ação
   Linha 18: if (pedido.Status == StatusPedido.Entregue)
   Linha 19:   return Result.Failure("...");

   Correção: mover para Pedido.Cancelar(motivo) em MinhaApp.Domain.
   Ação deve apenas orquestrar: buscar, chamar Domain, persistir.

   Referência: lma-v1.0.md §5 (Ação não decide regras)
              anti-patterns.md §5

─────────────────────────────────────────────────────
📄 MinhaApp.Domain/Modulos/Checkout/Pedido.cs

🔥 Domain importando EF Core
   Linha 1: using Microsoft.EntityFrameworkCore;

   Correção: remover. EF vive apenas em MinhaApp.Api.
   MinhaApp.Domain.csproj não deve ter PackageReference de EF.

─────────────────────────────────────────────────────
📋 Resumo:
  - {X} violações bloqueantes → corrigir antes do merge
  - {Y} avisos → revisar
  - {Z} arquivos sem problemas ✓

💡 Sugestões:
  - Use a skill `lma-refactor-to-rich` para mover regras para o Domain
  - Verifique lma-checklist-pr.md seção 8 (Domain)
```

### 5. Se nenhuma violação

```
✅ Revisão LMA v1.0 — {N} arquivos analisados
   Nenhuma violação encontrada. PR pode ser aprovado.
```

## Princípio guiador

Objetivo e acionável. Toda violação tem: arquivo + linha + correção. Sem filosofia.
