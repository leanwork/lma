---
name: lma-create-action
description: Criar uma Ação nova em projetos seguindo Lean Modular Architecture (LMA) v1.0 — padrão arquitetural .NET da Leanwork para APIs com Minimal API, EF Core e/ou Dapper. Use sempre que o usuário pedir para "criar ação", "criar action", "adicionar endpoint", "criar caso de uso", "novo slice", "criar CRUD", "adicionar funcionalidade", "criar feature", "criar comando", "criar query" em projetos .NET LMA, ou quando descrever uma operação de leitura/escrita e indicar que precisa virar código no padrão LMA. A skill conduz entrevista estruturada para identificar tipo da Ação (leitura EF, leitura Dapper, escrita CRUD anêmica, escrita com regra rica, ou escrita com gateway), módulo, entidade e rota; gera todos os arquivos do slice (Acao, Request, Response, Validator) no padrão LMA; registra no DI; e adiciona a rota no Endpoints/{Modulo}Endpoints.cs centralizado. Também identifica se a entidade do domínio precisa ser criada/refatorada para rica.
---

# LMA — Criar Ação

Geração de vertical slices (Ações) para projetos seguindo **Lean Modular Architecture (LMA) v1.0**.

## Princípios não-negociáveis

Antes de gerar qualquer código, internalize:

1. **Dois projetos físicos:** `MinhaApp.Domain` (puro) e `MinhaApp.Api` (web + infra). Ações vivem na `.Api`.
2. **Acesso direto a dados:** Ação injeta `AppDbContext` ou `IDbConnectionFactory` no construtor. **Não criar Repository nem Writer.**
3. **Roteamento centralizado:** rota da Ação vai em `Endpoints/{Modulo}Endpoints.cs`, **nunca** dentro da Ação.
4. **Protocolo web só no Endpoint:** Ação não conhece `HttpContext`, `ClaimsPrincipal` etc.
5. **Método público único:** `Execute(request, ct)` — sem variações.
6. **Validação automática:** FluentValidation roda no `IEndpointFilter`. Ação assume Request válido.
7. **Commit explícito:** Ação de escrita chama `await db.SaveChangesAsync(ct)` na última linha transacional.
8. **Result Pattern:** sempre retornar `Result<TResponse>`.
9. **Domain puro:** nada de `using Microsoft.EntityFrameworkCore`, `using MediatR`, `using Microsoft.AspNetCore.*` no projeto Domain.
10. **Nome da classe é verbo de negócio:** `ProcessarPedido`, `BuscarProdutos`. Sem sufixo `Handler`, `Service`, `UseCase`, `Reader`, `Writer`, `Command`, `Query`.

Se identificar qualquer violação ao gerar, **pare e corrija antes de continuar**.

## Workflow

### Etapa 1: Entrevista

Conduza entrevista estruturada **antes** de gerar código. Use `ask_user_input_v0` quando disponível, agrupando perguntas.

**Perguntas obrigatórias:**

1. **Tipo da Ação:**
   - Leitura simples/comum (EF Core, com filtros e paginação) → **Template A**
   - Leitura de alta performance (Dapper, relatório ou query complexa) → **Template B**
   - Escrita CRUD em entidade anêmica (criar/atualizar Cliente, Categoria, etc.) → **Template C**
   - Escrita com regra de negócio (cancelar, aprovar, publicar — vocabulário ubíquo) → **Template D**
   - Escrita com integração externa (gateway de pagamento, e-mail crítico, terceiro) → **Template E**

2. **Módulo (bounded context):** `Catalogo`, `Checkout`, `Clientes`, `Estoque`, `Auditoria`, etc. Se módulo não existe, sugerir invocar a skill `lma-add-module`.

3. **Nome da Ação:** verbo de negócio em Pascal (`ProcessarPedido`, `BuscarProdutos`, `CancelarPedido`). Sem sufixo `Handler`, `Service`, etc.

4. **Entidade envolvida:** entidade do Domain (`Pedido`, `Cliente`).

5. **Rota HTTP:** verbo e caminho (`POST /checkout/pedidos`, `GET /catalogo/produtos`).

**Perguntas condicionais (depende do tipo):**

- **Template A (leitura EF):** quais filtros? paginação? lista ou item único?
- **Template B (leitura Dapper):** qual o SQL? quais parâmetros?
- **Template C (escrita CRUD):** quais campos? confirmar que é anêmica (sem invariantes).
- **Template D (escrita com regra):** qual o nome do método de domínio (verbo ubíquo)? quais pré-condições? side effects?
- **Template E (escrita com gateway):** qual o gateway? estados intermediários? rollback condicional?

### Etapa 2: Validação cruzada

Antes de gerar, confirme com o usuário:

- Resumo dos arquivos que serão criados e seus caminhos
- Confirmar se entidade rica precisa ser criada/refatorada (Templates D e E)
- Confirmar se gateway já existe ou precisa ser criado (Template E)
- Confirmar se o módulo já tem `Endpoints/{Modulo}Endpoints.cs` ou precisa criar
- Confirmar se requer JWT/Claims (extração no Endpoint)

### Etapa 3: Geração

Use o template correspondente (ver `references/templates.md`).

**Ordem de geração:**

1. Se Templates D/E e entidade não é rica: refatorar/criar entidade rica primeiro em `MinhaApp.Domain/Modulos/{Modulo}/`.
2. Se Template E e gateway não existe: criar interface + implementação em `MinhaApp.Api/Infrastructure/Gateways/`.
3. Criar os arquivos do slice em `MinhaApp.Api/Modulos/{Modulo}/{Acao}/`:
   - `{Acao}.cs`
   - `{Acao}Request.cs`
   - `{Acao}Response.cs` (quando aplicável)
   - `{Acao}Validator.cs` (quando há entrada validável)
4. Atualizar/criar `Endpoints/{Modulo}Endpoints.cs` adicionando a rota.
5. Registrar a Ação no DI (`Program.cs` ou extension method do módulo).
6. Se módulo é novo: lembrar usuário de chamar `app.Map{Modulo}Endpoints()` no `Program.cs`.

### Etapa 4: Pós-geração

Após gerar:

1. **Apresente os arquivos criados** com `present_files` (quando disponível).
2. **Liste próximos passos manuais:**
   - Executar `dotnet build` para validar compilação
   - Rodar migration EF se houver mudança de schema (`dotnet ef migrations add`)
   - Adicionar teste de integração para a Ação
3. **Aponte trade-offs ou pendências.**

## Quando NÃO gerar

Pare e converse com o usuário se:

- A operação não cabe nos 5 templates (ex: background job, webhook recebido, integração via mensageria que não é gateway clássico)
- O nome proposto viola convenção (sem verbo, ou nome técnico tipo `ProcessarDadosHandler`)
- O domínio tem complexidade combinatória que sugere outra arquitetura
- A "Ação" descrita são na verdade várias coisas (sugerir quebrar em Ações separadas)

## Referências

- `references/templates.md` — os 5 templates completos (A: leitura EF, B: leitura Dapper, C: escrita CRUD, D: escrita com regra, E: escrita com gateway)
- `references/anti-patterns.md` — anti-padrões a evitar durante a geração
- `references/checklist.md` — checklist aplicado após gerar
- `/docs/lma-v1.0.md` (no plugin) — documento arquitetural completo

Consulte `templates.md` **sempre** antes de gerar — não confie em memória.

## Checklist de qualidade (aplicar antes de entregar)

Após gerar todos os arquivos, valide internamente:

- [ ] Classe da Ação **não** tem sufixo `Handler`, `Service`, `UseCase`, `Reader`, `Writer`, `Command`, `Query`
- [ ] Método público único se chama `Execute`
- [ ] Ação retorna `Result<TResponse>` ou `Result`
- [ ] Ação **não** importa `Microsoft.AspNetCore.*` nem injeta `ClaimsPrincipal`/`HttpContext`
- [ ] Ação injeta `AppDbContext`, `IDbConnectionFactory` ou gateway por interface — **não** injeta Repository (que não existe)
- [ ] Leitura usa `AsNoTracking()` e projeta via `Select(...)` para Response
- [ ] Escrita chama `SaveChangesAsync(ct)` na última linha transacional
- [ ] Validator existe se há input validável e usa FluentValidation
- [ ] Rota adicionada em `Endpoints/{Modulo}Endpoints.cs` (não dentro da Ação)
- [ ] Ação registrada no DI
- [ ] Domain (se modificado) **não** importa nada de infra

Se qualquer item falhar, **corrija antes de apresentar ao usuário**.

## Tom e comunicação

- **PT-BR** com termos técnicos em EN (Ação, Endpoint, Domain, Module, Validator, Gateway).
- Direto: o usuário é dev experiente ou junior aprendendo LMA.
- Cite o princípio do LMA por trás de cada decisão.
- Não invente variações dos templates — se algo não couber, **explique e pergunte**.
