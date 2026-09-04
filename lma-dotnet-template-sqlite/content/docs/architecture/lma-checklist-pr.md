# LMA — Checklist de Pull Request

> Checklist para revisão de PRs em projetos seguindo LMA v1.0.
> Documento principal: `docs/architecture/lma-v1.0.md`
> Templates: `docs/architecture/lma-templates.md`

## Como usar

- **Revisor:** marque cada item antes de aprovar o PR. Qualquer item desmarcado é bloqueante.
- **Autor:** revise você mesmo antes de pedir revisão.
- **Líder técnico:** itens marcados com 🔥 são gatilho automático de rejeição.

---

## 1. Estrutura física dos projetos

- 🔥 [ ] `MinhaApp.Domain.csproj` **não** tem `PackageReference` para EF Core, MediatR, FluentValidation, ASP.NET ou qualquer infra
- 🔥 [ ] `MinhaApp.Api.csproj` referencia `MinhaApp.Domain` (e nunca o contrário)
- [ ] Arquivos do Domain estão em `MinhaApp.Domain/Modulos/{Modulo}/`
- [ ] Arquivos da API estão em `MinhaApp.Api/{Endpoints|Modulos|Infrastructure|Common}/`

## 2. Estrutura da Ação

- [ ] Ação está em pasta única sob `MinhaApp.Api/Modulos/{Modulo}/{Acao}/`
- [ ] A pasta da Ação é **plana** — não há subpastas como `Application/`, `UseCases/`, `Handlers/`, `Domain/`
- [ ] Nome da classe é o verbo de negócio (`ProcessarPedido`, `BuscarProdutos`) — **sem** sufixo `Handler`, `Service`, `UseCase`, `Reader`, `Writer`, `Command`, `Query`
- [ ] Arquivos seguem o padrão: `{Acao}.cs`, `{Acao}Request.cs`, `{Acao}Response.cs`, `{Acao}Validator.cs`
- [ ] **Método público único** da Ação se chama **`Execute`**
- [ ] `Execute` retorna `Result<TResponse>` (ou `Result` quando não há valor)

## 3. Endpoint e roteamento

- 🔥 [ ] Rota é declarada em `Endpoints/{Modulo}Endpoints.cs`, **não** dentro da Ação
- 🔥 [ ] A Ação **não** tem método `Map`, **não** referencia `IEndpointRouteBuilder`, **não** conhece HTTP
- [ ] O grupo de endpoints do módulo aplica `.AddEndpointFilter<ValidationFilter>()`
- [ ] O grupo aplica `.RequireAuthorization()` ou política equivalente quando aplicável
- [ ] Lambda do endpoint tem menos de 15 linhas
- [ ] Endpoint mapeia `Result` para `Results.Ok/BadRequest/NoContent` apropriadamente

## 4. Isolamento do protocolo web

- 🔥 [ ] Ação **não** injeta `IHttpContextAccessor`, `HttpContext`, `ClaimsPrincipal` ou qualquer tipo de `Microsoft.AspNetCore.*`
- 🔥 [ ] Ação **não** tem `using Microsoft.AspNetCore.*`
- [ ] `ClienteId` (ou outros IDs do usuário autenticado) é extraído no Endpoint via `user.FindFirstValue(...)` e passado limpo no Request
- [ ] Request usa `init` ou `with` para receber o ID injetado pelo Endpoint
- [ ] Headers, cookies, claims são lidos apenas no Endpoint

## 5. Acesso a dados (sem Repository)

- 🔥 [ ] **Não** há `IRepository<T>`, `IPedidoRepository`, `IClienteRepository` ou interfaces equivalentes de persistência
- 🔥 [ ] **Não** há classes `Repository`, `Writer` ou `Reader` em `Infrastructure/`
- [ ] Ação injeta `AppDbContext` diretamente quando usa EF Core
- [ ] Ação injeta `IDbConnectionFactory` (ou equivalente) diretamente quando usa Dapper
- [ ] Queries Dapper usam parâmetros nomeados (`@param`), **não** interpolação de string

## 6. Leitura

- [ ] Queries de leitura via EF usam `AsNoTracking()`
- [ ] Projeção via `Select(x => new {Acao}Response(...))` — **não** retorna entidade do Domain como Response
- [ ] Filtros condicionais (`if (... is not null) query = query.Where(...)`)
- [ ] Paginação com `Skip` + `Take` em listagens que podem ser grandes
- [ ] Ordenação explícita (`OrderBy*`) em queries que retornam lista

## 7. Escrita

- 🔥 [ ] Ação de escrita chama `await db.SaveChangesAsync(ct)` explicitamente (sem isso, nada é persistido)
- [ ] `SaveChangesAsync` é a **última linha** da execução transacional (ou último passo antes do `return`)
- [ ] Transações que envolvem gateway externo usam `db.Database.BeginTransactionAsync(ct)` explícito quando há rollback condicional
- [ ] Se há regra de negócio, ela vive no método do Domain rico, **não** no Handler — a Ação apenas orquestra
- [ ] `Add`, `Update`, `Remove` no DbSet aparecem na Ação (não em Repository inexistente)

## 8. Domain

- 🔥 [ ] Arquivos em `MinhaApp.Domain/**` **não** têm `using Microsoft.EntityFrameworkCore`, `using MediatR`, `using Microsoft.AspNetCore.*`, `using FluentValidation`
- 🔥 [ ] `MinhaApp.Domain.csproj` continua sem `PackageReference` de infra
- [ ] Entidades ricas têm setters privados em propriedades de comportamento
- [ ] Entidades ricas têm construtor privado e factory `Criar(...)`
- [ ] Métodos de domínio usam **vocabulário ubíquo** (`Cancelar`, `Aprovar`, `Publicar`) — não `SetStatus`, `UpdateXxx`, `Process`
- [ ] Métodos de domínio com pré-condições retornam `Result`
- [ ] Coleções expostas como `IReadOnlyList<T>` com backing field privado
- [ ] Domain Services (quando existem) vivem em `MinhaApp.Domain/Modulos/{Modulo}/`

## 9. Validação

- 🔥 [ ] Validação manual dentro da Ação **não** existe (FluentValidation roda no `IEndpointFilter` antes)
- [ ] Validador de formato/obrigatoriedade usa **FluentValidation** em `{Acao}Validator`
- [ ] Validador de regra de negócio vive no método do Domain rico, retornando `Result`
- [ ] Validators registrados via `AddValidatorsFromAssembly(...)` ou equivalente
- [ ] `ValidationFilter` está registrado e aplicado no grupo de endpoints

## 10. Result Pattern

- 🔥 [ ] Ação **não** joga exception para representar falha de regra de negócio
- [ ] `Result.Success(...)` e `Result.Failure(...)` são usados consistentemente
- [ ] Endpoint traduz `Result` para HTTP (não joga exception ao verificar `IsFailure`)
- [ ] `Result` do Domain e da API é o mesmo tipo (recomendado: viver no `Domain/_Common/`)

## 11. DTOs

- [ ] DTOs (Request, Response) vivem **dentro do slice** da Ação, não em pasta `DTOs/` central
- [ ] DTOs são `record` (não `class` mutável)
- [ ] Response **não** expõe entidade do Domain — sempre projeta para tipo da Ação
- [ ] Request usa tipos primitivos ou value objects do domínio (não entidades)

## 12. Acoplamento entre Ações

- 🔥 [ ] **Nenhuma** Ação injeta outra Ação no construtor
- [ ] Lógica compartilhada entre Ações vive em Domain Service no `MinhaApp.Domain/Modulos/{Modulo}/`
- [ ] Acesso a dados de outro módulo é feito via query direta no DbContext (cruzando módulos no nível de dados), **não** chamando Ação do outro módulo
- [ ] Coordenação entre módulos (quando necessário) acontece via eventos ou serviço de domínio compartilhado

## 13. Infraestrutura externa (gateways)

- [ ] Integrações externas (pagamento, storage, e-mail, mensageria) têm interface em `Infrastructure/Gateways/`, `Storage/`, `Email/`, etc.
- [ ] Interface é específica e pequena (`IPagamentoGateway`, `IArmazenamentoImagens`) — **não** genérica
- [ ] Implementação concreta registrada no DI por interface
- [ ] Ação injeta a interface, nunca a implementação concreta

## 14. Composition Root (DI)

- [ ] Toda Ação está registrada (via `AddScoped<{Acao}>()` linha-a-linha ou scan de assembly)
- [ ] Gateways/Storage/Email registrados pela interface (`AddScoped<IPagamentoGateway, PagarMeGateway>()`)
- [ ] Validators registrados via `AddValidatorsFromAssembly(...)`
- [ ] `Program.cs` apenas compõe módulos — **não** tem mapeamento de rota solto (exceto `/health` e similares)
- [ ] Endpoints chamados via `app.Map{Modulo}Endpoints()`

## 15. Testes

- [ ] **Domain** tem teste unitário puro (sem mocks, sem banco)
- [ ] **Ações** têm teste de integração via `WebApplicationFactory` + Testcontainers (banco real em Docker)
- [ ] **Não** há mock de `DbContext`
- [ ] **Não** há mock de Repository (que não existe na LMA)
- [ ] Estrutura de testes espelha `MinhaApp.Domain/Modulos/` e `MinhaApp.Api/Modulos/`
- [ ] Testes de Ação fazem chamadas HTTP reais ao `HttpClient` da factory

## 16. Proibições explícitas

- 🔥 [ ] **Nenhum** `using MediatR` em qualquer arquivo
- 🔥 [ ] **Nenhum** `using AutoMapper` em qualquer arquivo
- 🔥 [ ] **Nenhum** Controller MVC (`: ControllerBase`) na solução
- 🔥 [ ] **Nenhum** sufixo `Handler`, `Service`, `UseCase`, `Reader`, `Writer`, `Command`, `Query` no nome das Ações
- [ ] **Nenhum** `IRepository<T>`, `IUnitOfWork` genérico, ou abstração de DbContext "por via das dúvidas"

## 17. Geral

- [ ] Todos os métodos `async` recebem e propagam `CancellationToken`
- [ ] Sem `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` em código de produção
- [ ] Migrations EF criadas e revisadas quando há mudança de schema
- [ ] Connection strings em `appsettings.{Environment}.json`, **não** hardcoded
- [ ] Secrets via `dotnet user-secrets` (dev) ou Key Vault/Variáveis de ambiente (prod)

---

## Gatilhos automáticos de rejeição (🔥)

Os itens marcados com 🔥 ao longo do documento são **bloqueantes em qualquer circunstância**:

1. Domain referenciando framework de infra (csproj ou using)
2. Ação com sufixo `Handler`/`Service`/`UseCase`/`Reader`/`Writer`/`Command`/`Query`
3. Ação declarando própria rota ou conhecendo HTTP
4. Ação injetando outra Ação
5. Reintroduzir Repository ou Writer
6. Validação manual dentro da Ação
7. Ação injetando `HttpContext`/`ClaimsPrincipal`
8. Uso de MediatR ou AutoMapper

Se qualquer um desses falhar, o PR é rejeitado sem necessidade de revisão linha-a-linha. O autor corrige e reabre.

---

## Quando o checklist não se aplica

Algumas operações podem **não** se encaixar nos templates A-E:

- **Jobs e workers de background** — Ação sem Endpoint, registrada como `IHostedService` ou disparada por agendador. Aplique seções 1, 2 (sem 3), 4, 5, 7, 8, 12, 14, 16, 17.
- **Webhooks de terceiros** — Endpoint + Ação, mas Request vem do parceiro (não validar como entrada de usuário; validar assinatura HMAC se aplicável).
- **Health checks e endpoints triviais** — podem ficar como lambda pura em `Program.cs`. Aplicar apenas seções 1, 8, 16, 17.
- **Migrations EF e seed de dados** — não passam por Ação/Endpoint. Aplicar seções 1, 8, 16, 17.

Em casos limítrofes, consulte `docs/architecture/lma-v1.0.md` ou discuta com o arquiteto antes de abrir o PR.
