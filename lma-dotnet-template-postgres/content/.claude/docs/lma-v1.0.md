# Lean Modular Architecture (LMA)

**Versão:** 1.0
**Autor:** Leanwork Group
**Status:** Oficial

> **Arquitetura modular para APIs .NET que pensam em produtividade humana e de IA.**
>
> Dois projetos, módulos de negócio, Ações como vertical slices, acesso direto a dados, estrutura previsível. Vale para MVP de uma pessoa em duas semanas e para sistema com time de 15+ devs em anos de evolução. Evolução consolidada da Lean Vertical Architecture (LVA).

---

## Sumário

1. [Filosofia e contexto](#1-filosofia-e-contexto)
2. [Estrutura física de projetos](#2-estrutura-física-de-projetos)
3. [Os três pilares da API](#3-os-três-pilares-da-api)
4. [Estrutura de pastas oficial](#4-estrutura-de-pastas-oficial)
5. [Anatomia de uma Ação](#5-anatomia-de-uma-ação)
6. [Roteamento centralizado por módulo](#6-roteamento-centralizado-por-módulo)
7. [Acesso a dados direto](#7-acesso-a-dados-direto)
8. [Isolamento do protocolo web](#8-isolamento-do-protocolo-web)
9. [Validação automática via IEndpointFilter](#9-validação-automática-via-iendpointfilter)
10. [Result Pattern](#10-result-pattern)
11. [Política de commit](#11-política-de-commit)
12. [Domain rico e Domain Services](#12-domain-rico-e-domain-services)
13. [Independência entre fatias](#13-independência-entre-fatias)
14. [Estratégia de testes](#14-estratégia-de-testes)
15. [SOLID na LMA](#15-solid-na-lma)
16. [Otimizada para IA](#16-otimizada-para-ia)
17. [Anti-padrões](#17-anti-padrões)
18. [Quando NÃO usar LMA](#18-quando-não-usar-lma)
19. [FAQ](#19-faq)
20. [Glossário](#20-glossário)

---

## 1. Filosofia e contexto

A Lean Modular Architecture é uma arquitetura **API-first** desenhada para sistemas .NET que precisam ir direto ao ponto, sem cerimônia teórica. Vale tanto para um MVP construído por uma pessoa em duas semanas quanto para um sistema de longa vida com time grande e milhares de endpoints — a estrutura é a mesma, o que muda é a quantidade de módulos.

**Princípio fundador:**

> **Separe por módulo de negócio e por ação. Isole o que muda de verdade — e o que muda é a infraestrutura externa, não o banco. Otimize para que a estrutura seja previsível e gerável.**

Três ideias estão embutidas nesse princípio:

**1. Modularidade por negócio.** O código se organiza por bounded context (Catálogo, Checkout, Clientes, Faturamento, Auditoria), não por camada técnica. Cada módulo é coeso, conhece seu próprio vocabulário e seu próprio modelo de dados.

**2. Isolamento seletivo.** Clean Architecture isola tudo "por via das dúvidas". LMA isola só o que realmente troca: gateways de pagamento, provedores de storage, mensageria, serviços externos. **Não cria abstração de persistência**, porque o banco raramente troca de verdade — e quando troca, é decisão de negócio que justifica refactor consciente, não abstração preventiva.

**3. Otimização para produtividade humana E de IA.** Essa é uma diferença fundamental da LMA em relação a arquiteturas pensadas há 15-20 anos. Boa parte do código hoje é escrita ou assistida por LLMs (Claude Code, Cursor, Copilot). A LMA assume isso como premissa de design: a estrutura é **previsível**, o vocabulário é **fixo**, as decisões já estão **tomadas**. Isso reduz a ambiguidade que faz IA inventar variações, e dá guardrails (como os dois projetos físicos) que o compilador valida automaticamente. Detalhes em §16.

### Para quem a LMA serve

- APIs .NET que serão o produto, não detalhe técnico
- Projetos rápidos (MVP, prova de conceito, SaaS inicial) que precisam de qualidade desde o dia 1
- Projetos complexos e duradouros (ERP, marketplace, fintech, plataforma B2B) que precisam evoluir sem virar bagunça
- Times mistos onde IA participa ativamente da geração de código
- Sistemas modulares que podem ter múltiplos bancos de dados, um por módulo (ver §7)

### Evolução em relação ao LVA

LMA consolida a Lean Vertical Architecture (LVA) com cinco mudanças estruturais:

1. Dois projetos físicos obrigatórios desde o dia 1 (compilador protege as regras)
2. Roteamento centralizado por módulo (não mais um Endpoint por feature)
3. Unificação de Reader/Handler em uma única classe de **Ação**
4. Eliminação completa de Repository/Writer — acesso direto a dados na Ação
5. Validação automática via `IEndpointFilter` global

Cada uma dessas mudanças tem dupla justificativa: simplifica para o desenvolvedor humano **e** reduz superfície de erro para geração por IA.

---

## 2. Estrutura física de projetos

O sistema é dividido obrigatoriamente em **dois projetos** (`.csproj`) desde o primeiro dia. Essa separação física faz o compilador ser o guardião das regras de dependência — não é convenção, é barreira de build.

### `MinhaApp.Domain`

Projeto **puramente conceitual**, isolado e agnóstico a frameworks.

- **Não referencia** Entity Framework, Dapper, HTTP, FluentValidation ou qualquer ferramenta de infraestrutura.
- Contém apenas: entidades ricas, Value Objects e Domain Services.
- Organizado por módulos de negócio.

```xml
<!-- MinhaApp.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <!-- ZERO PackageReference de infraestrutura. Projeto puro. -->
</Project>
```

### `MinhaApp.Api`

O ponto de entrada do sistema.

- Contém a camada web, as classes de Ação, o mapeamento de rotas e toda a infraestrutura técnica.
- **Referencia** `MinhaApp.Domain`.

```xml
<!-- MinhaApp.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\MinhaApp.Domain\MinhaApp.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.*" />
    <PackageReference Include="Dapper" Version="2.*" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
  </ItemGroup>
</Project>
```

### A regra de dependência (única e absoluta)

```
MinhaApp.Api  ──referencia──▶  MinhaApp.Domain
MinhaApp.Domain  ──referencia──▶  (nada de infraestrutura)
```

O Domain nunca conhece a API. A API conhece o Domain. Como são projetos separados, **tentar importar EF no Domain não compila** — o erro aparece no build, não no code review.

---

## 3. Os três pilares da API

A `MinhaApp.Api` é organizada em três pilares fundamentais, cada um uma pasta raiz:

### Endpoints (Roteamento Centralizado)

Pasta `Endpoints/` com arquivos centralizadores por módulo (`CheckoutEndpoints.cs`, `CatalogoEndpoints.cs`). Usam extension methods de Minimal API para agrupar e direcionar requisições HTTP para as classes de Ação. **É aqui que o protocolo web é resolvido** (JWT, Claims, headers).

### Modulos (Ações / Vertical Slices)

Pasta `Modulos/`. Dentro de cada módulo, cada subpasta representa uma **Ação única** baseada no verbo de negócio. A pasta é plana e contém: a classe de operação (Ação), Request, Response e Validator.

### Infrastructure (Ferramentas externas)

Pasta `Infrastructure/` que centraliza tudo acoplado a frameworks ou I/O externo: banco de dados, gateways de pagamento, storage, e-mail.

---

## 4. Estrutura de pastas oficial

```
src/
├── MinhaApp.Domain/
│   └── Modulos/
│       ├── Clientes/
│       │   └── Cliente.cs              (entidade pura)
│       └── Checkout/
│           ├── Pedido.cs               (entidade rica pura)
│           ├── ItemPedido.cs
│           └── PoliticaDesconto.cs     (Domain Service, se compartilhado)
│
└── MinhaApp.Api/
    ├── Endpoints/
    │   ├── ClienteEndpoints.cs         (mapeia todas as rotas de clientes)
    │   └── CheckoutEndpoints.cs        (mapeia todas as rotas de checkout)
    │
    ├── Modulos/
    │   ├── Catalogo/
    │   │   └── BuscarProdutos/
    │   │       ├── BuscarProdutos.cs           (classe de Ação)
    │   │       ├── BuscarProdutosRequest.cs
    │   │       ├── BuscarProdutosResponse.cs
    │   │       └── BuscarProdutosValidator.cs
    │   └── Checkout/
    │       └── ProcessarPedido/
    │           ├── ProcessarPedido.cs          (classe de Ação)
    │           ├── ProcessarPedidoRequest.cs
    │           ├── ProcessarPedidoResponse.cs
    │           └── ProcessarPedidoValidator.cs
    │
    ├── Infrastructure/
    │   ├── Database/                   (AppDbContext.cs, fábricas Dapper, Migrations)
    │   ├── Gateways/                   (PagarMe, Stripe, etc.)
    │   ├── Storage/                    (arquivos e imagens de produtos)
    │   └── Email/                      (e-mail transacional)
    │
    ├── Common/
    │   ├── Result.cs                   (Result Pattern)
    │   └── ValidationFilter.cs         (IEndpointFilter global)
    │
    └── Program.cs
```

**Nota sobre Domain Services:** ficam em `MinhaApp.Domain/Modulos/{Modulo}/` junto das entidades do módulo a que pertencem. Domain Services que cruzam módulos podem viver em um `Modulos/_Shared/` no Domain.

---

## 5. Anatomia de uma Ação

A Ação é a unidade central da LMA. Substitui os antigos Handler e Reader — agora há uma única classe por vertical slice, com método público `Execute()`.

### Ação de escrita (com regra de negócio)

```csharp
// Modulos/Checkout/ProcessarPedido/ProcessarPedido.cs
namespace MinhaApp.Api.Modulos.Checkout.ProcessarPedido;

public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<ProcessarPedidoResponse>> Execute(
        ProcessarPedidoRequest request,
        CancellationToken ct)
    {
        // request já chegou validado (IEndpointFilter)
        // e com ClienteId limpo (extraído no Endpoint)

        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId, ct);
        if (cliente is null)
            return Result.Failure<ProcessarPedidoResponse>("Cliente não encontrado");

        // regra de negócio mora no Domain rico
        var pedido = Pedido.Criar(request.ClienteId, request.Itens);

        // integração externa isolada via gateway
        var cobranca = await gateway.CobrarAsync(pedido.Total, ct);
        if (cobranca.IsFailure)
            return Result.Failure<ProcessarPedidoResponse>(cobranca.Error);

        pedido.ConfirmarPagamento(cobranca.TransacaoId);

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);   // commit explícito, última linha transacional

        return Result.Success(new ProcessarPedidoResponse(pedido.Id, pedido.Total));
    }
}
```

### Ação de leitura

```csharp
// Modulos/Catalogo/BuscarProdutos/BuscarProdutos.cs
namespace MinhaApp.Api.Modulos.Catalogo.BuscarProdutos;

public class BuscarProdutos(AppDbContext db)
{
    public async Task<Result<BuscarProdutosResponse>> Execute(
        BuscarProdutosRequest request,
        CancellationToken ct)
    {
        var query = db.Produtos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Termo))
            query = query.Where(p => p.Nome.Contains(request.Termo));

        if (request.CategoriaId is not null)
            query = query.Where(p => p.CategoriaId == request.CategoriaId);

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderBy(p => p.Nome)
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(p => new ProdutoItem(p.Id, p.Nome, p.Preco, p.ImagemUrl))
            .ToListAsync(ct);

        return Result.Success(new BuscarProdutosResponse(itens, total, request.Pagina, request.Tamanho));
    }
}
```

### Request, Response e Validator

```csharp
// ProcessarPedidoRequest.cs
public record ProcessarPedidoRequest(
    List<ItemPedidoDto> Itens,
    string FormaPagamento)
{
    // ClienteId é preenchido pelo Endpoint a partir do JWT.
    // Vem como init para o Endpoint conseguir setar via 'with'.
    public Guid ClienteId { get; init; }
}

// ProcessarPedidoResponse.cs
public record ProcessarPedidoResponse(Guid PedidoId, decimal Total);

// ProcessarPedidoValidator.cs
public class ProcessarPedidoValidator : AbstractValidator<ProcessarPedidoRequest>
{
    public ProcessarPedidoValidator()
    {
        RuleFor(x => x.Itens).NotEmpty().WithMessage("Pedido deve ter ao menos um item");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
            item.RuleFor(i => i.ProdutoId).NotEmpty();
        });
        RuleFor(x => x.FormaPagamento).NotEmpty();
    }
}
```

### Regras da Ação

- Método público único: **`Execute(request, ct)`**
- Retorna sempre **`Result<TResponse>`**
- Injeta `AppDbContext` (ou conexão Dapper) e gateways de infra **diretamente** no construtor
- Assume que o Request **já chegou validado** (o filtro garante isso)
- Assume que dados de protocolo web (ClienteId) **já vêm limpos** no Request
- Nunca injeta outra Ação
- Ação de escrita chama `SaveChangesAsync` na última linha

---

## 6. Roteamento centralizado por módulo

Diferente de arquiteturas onde cada feature declara a própria rota, na LMA o roteamento é **centralizado por módulo** em arquivos na pasta `Endpoints/`. A Ação não conhece sua rota — quem conhece é o arquivo de Endpoints do módulo.

```csharp
// Endpoints/CheckoutEndpoints.cs
namespace MinhaApp.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/checkout")
            .WithTags("Checkout")
            .AddEndpointFilter<ValidationFilter>()    // validação automática
            .RequireAuthorization();

        grupo.MapPost("/pedidos", async (
            ProcessarPedidoRequest body,
            ClaimsPrincipal user,                     // protocolo web AQUI
            ProcessarPedido acao,
            CancellationToken ct) =>
        {
            var clienteId = Guid.Parse(user.FindFirstValue("sub")!);
            var request = body with { ClienteId = clienteId };

            var resultado = await acao.Execute(request, ct);
            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Results.BadRequest(resultado.Error);
        });

        grupo.MapPost("/pedidos/{id:guid}/cancelar", async (
            Guid id,
            CancelarPedidoRequest body,
            ClaimsPrincipal user,
            CancelarPedido acao,
            CancellationToken ct) =>
        {
            var clienteId = Guid.Parse(user.FindFirstValue("sub")!);
            var request = body with { PedidoId = id, ClienteId = clienteId };

            var resultado = await acao.Execute(request, ct);
            return resultado.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(resultado.Error);
        });

        return app;
    }
}
```

```csharp
// Program.cs — composição dos módulos
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Validators registrados automaticamente
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Ações registradas (uma linha por Ação, ou via scan de assembly)
builder.Services.AddScoped<ProcessarPedido>();
builder.Services.AddScoped<CancelarPedido>();
builder.Services.AddScoped<BuscarProdutos>();

// Gateways de infra
builder.Services.AddScoped<IPagamentoGateway, PagarMeGateway>();

var app = builder.Build();

app
    .MapCheckoutEndpoints()
    .MapCatalogoEndpoints()
    .MapClienteEndpoints();

app.Run();
```

**Por que centralizar o roteamento:** num e-commerce com dezenas de Ações por módulo, ter o mapa de rotas de um módulo em um arquivo único facilita auditoria, versionamento de API, aplicação de políticas transversais (auth, rate limit, filtros) por grupo, e leitura rápida de "tudo que o Checkout expõe".

---

## 7. Acesso a dados direto

Esta é a decisão mais marcante da LMA. **Não existe Repository nem Writer.** A classe de Ação injeta `AppDbContext` (EF Core) ou a conexão Dapper diretamente no construtor, para leitura E escrita.

```csharp
// Escrita com EF Core
public class CriarCategoria(AppDbContext db)
{
    public async Task<Result<CriarCategoriaResponse>> Execute(
        CriarCategoriaRequest request, CancellationToken ct)
    {
        var categoria = new Categoria { Id = Guid.NewGuid(), Nome = request.Nome };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        return Result.Success(new CriarCategoriaResponse(categoria.Id));
    }
}
```

```csharp
// Leitura de alta performance com Dapper
public class RelatorioVendas(IDbConnectionFactory connectionFactory)
{
    public async Task<Result<RelatorioVendasResponse>> Execute(
        RelatorioVendasRequest request, CancellationToken ct)
    {
        using var conn = await connectionFactory.CreateAsync(ct);
        var linhas = await conn.QueryAsync<LinhaRelatorio>(
            """
            SELECT CategoriaId, SUM(Total) AS Faturamento, COUNT(*) AS Pedidos
            FROM Pedidos
            WHERE CriadoEm BETWEEN @De AND @Ate
            GROUP BY CategoriaId
            """,
            new { request.De, request.Ate });

        return Result.Success(new RelatorioVendasResponse(linhas.ToList()));
    }
}
```

### O que essa decisão implica

**Ganha-se:**
- Menos camadas, menos cerimônia, menos código
- Liberdade para usar EF onde produtividade importa e Dapper onde performance importa, na mesma Ação se preciso
- Queries de leitura otimizadas sem passar por abstração de agregado
- **Flexibilidade para múltiplos bancos por módulo** — uma Ação do módulo Catálogo pode injetar um `CatalogoDbContext` apontando para Postgres, enquanto uma Ação do módulo Auditoria injeta uma conexão Mongo, e uma Ação de Analytics injeta uma conexão ClickHouse. Cada módulo escolhe o banco que faz sentido para seu workload, sem que isso vaze para outros módulos. Detalhes na FAQ.

**Abre-se mão de:**
- Troca plug-and-play de mecanismo de persistência por agregado (não há interface a reimplementar)
- A fronteira DIP de persistência que existia no LVA

Coerente com a premissa: o banco raramente troca em um módulo já estabelecido. O que troca — gateways, storage, e-mail, mensageria — **continua isolado por interface** em `Infrastructure/`.

### O que continua isolado por interface

Infraestrutura externa volátil **mantém** abstração, porque essa troca acontece de verdade:

```csharp
// Domain NÃO conhece essas interfaces — elas vivem na API, em Infrastructure/
public interface IPagamentoGateway
{
    Task<ResultadoCobranca> CobrarAsync(decimal valor, CancellationToken ct);
}

public interface IArmazenamentoImagens
{
    Task<string> SalvarAsync(Stream imagem, string nome, CancellationToken ct);
}
```

A Ação injeta essas interfaces; a implementação concreta (PagarMe, Azure Blob) é registrada no DI. **Persistência é exceção à regra de abstração; integração externa não é.**

---

## 8. Isolamento do protocolo web

Lógicas de contexto web — extrair ID do usuário autenticado de tokens JWT ou Claims, ler headers, lidar com `HttpContext` — são resolvidas **estritamente no nível do Endpoint**.

O Endpoint extrai o dado e passa o ID limpo (`Guid ClienteId`) dentro do objeto de Request para o método `Execute()` da Ação.

```csharp
// No Endpoint — protocolo web vive aqui
grupo.MapPost("/pedidos", async (
    ProcessarPedidoRequest body,
    ClaimsPrincipal user,
    ProcessarPedido acao,
    CancellationToken ct) =>
{
    var clienteId = Guid.Parse(user.FindFirstValue("sub")!);  // ← extração do JWT
    var request = body with { ClienteId = clienteId };        // ← ID limpo no Request
    var resultado = await acao.Execute(request, ct);
    return resultado.IsSuccess ? Results.Ok(resultado.Value) : Results.BadRequest(resultado.Error);
});
```

```csharp
// Na Ação — NUNCA vê HttpContext, ClaimsPrincipal ou nada de web
public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<ProcessarPedidoResponse>> Execute(
        ProcessarPedidoRequest request, CancellationToken ct)
    {
        // request.ClienteId já é um Guid limpo. A Ação não sabe de onde veio.
    }
}
```

**Por que isso importa:** a Ação fica testável sem montar `HttpContext` falso, e o conhecimento de "como o usuário se autentica" fica concentrado em um lugar. Trocar JWT por outro esquema de auth afeta só os Endpoints.

---

## 9. Validação automática via IEndpointFilter

A validação com FluentValidation é executada **automaticamente** na camada HTTP, através de um `IEndpointFilter` global aplicado no mapeamento dos grupos de endpoints. Se o Request falhar nas regras do Validator daquela Ação, o filtro intercepta e retorna `400 BadRequest` **antes** de permitir entrada na classe de Ação.

```csharp
// Common/ValidationFilter.cs
public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Encontra o argumento que é um Request com validator registrado
        var request = context.Arguments
            .FirstOrDefault(a => a is not null && a.GetType().Name.EndsWith("Request"));

        if (request is not null)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                var validationContext = new ValidationContext<object>(request);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
```

Aplicado no grupo:

```csharp
var grupo = app.MapGroup("/checkout")
    .AddEndpointFilter<ValidationFilter>();   // toda rota do grupo valida automaticamente
```

**Consequência para a Ação:** o método `Execute()` assume que o Request chegou válido. Não há revalidação dentro da Ação. Validações de formato/obrigatoriedade vivem no Validator; validações de regra de negócio vivem no Domain rico.

---

## 10. Result Pattern

A LMA adota estritamente o **Result Pattern** (`Result<TResponse>`). O método `Execute()` sempre retorna um objeto de resultado indicando sucesso ou falha de negócio. O Endpoint avalia esse retorno para decidir o Status Code HTTP.

```csharp
// Common/Result.cs
namespace MinhaApp.Api.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        Value = value;
    }
}
```

### Tradução Result → HTTP no Endpoint

```csharp
var resultado = await acao.Execute(request, ct);
return resultado.IsSuccess
    ? Results.Ok(resultado.Value)
    : Results.BadRequest(resultado.Error);
```

Para mapeamentos mais ricos (404 vs 400 vs 409), pode-se estender `Result` com um tipo de erro, ou usar um helper de tradução. Para começar, `Ok`/`BadRequest` cobre a maioria dos casos.

**Result vs Exception:** `Result` para falhas esperadas de negócio ("cliente não encontrado", "estoque insuficiente"). `Exception` para falhas inesperadas (banco fora, timeout, bug). Exception não é fluxo de controle.

---

## 11. Política de commit

O salvamento é **explícito**. A classe de Ação de escrita é responsável por chamar `await db.SaveChangesAsync(ct)` na sua última linha de execução transacional.

```csharp
public async Task<Result<ProcessarPedidoResponse>> Execute(
    ProcessarPedidoRequest request, CancellationToken ct)
{
    // ... lógica, validações de negócio, integração ...

    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync(ct);   // ← explícito, última linha transacional

    return Result.Success(new ProcessarPedidoResponse(pedido.Id, pedido.Total));
}
```

**Por que explícito e não via filtro/middleware automático:** num e-commerce, a ordem de operações importa — às vezes você precisa persistir, depois chamar gateway, depois persistir de novo. Commit automático no fim do request esconde esse controle. Tornar explícito deixa a transação visível e sob controle da Ação.

**Sobre transações que cruzam operações:** o `AppDbContext` é a unidade de trabalho. Múltiplas mudanças antes de um único `SaveChangesAsync` são uma transação. Se precisar de controle transacional mais fino (ex: rollback após falha de gateway), use `db.Database.BeginTransactionAsync(ct)` explicitamente.

---

## 12. Domain rico e Domain Services

O projeto `MinhaApp.Domain` contém a lógica de negócio pura.

### Entidades ricas

Protegem invariantes através de métodos com vocabulário ubíquo, setters privados e construtor privado.

```csharp
// MinhaApp.Domain/Modulos/Checkout/Pedido.cs
namespace MinhaApp.Domain.Modulos.Checkout;

public class Pedido
{
    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public StatusPedido Status { get; private set; }
    public decimal Total { get; private set; }
    public string? TransacaoId { get; private set; }

    private readonly List<ItemPedido> _itens = new();
    public IReadOnlyList<ItemPedido> Itens => _itens.AsReadOnly();

    private Pedido() { }

    public static Pedido Criar(Guid clienteId, IEnumerable<ItemPedido> itens)
    {
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Status = StatusPedido.AguardandoPagamento
        };
        pedido._itens.AddRange(itens);
        pedido.RecalcularTotal();
        return pedido;
    }

    public void ConfirmarPagamento(string transacaoId)
    {
        Status = StatusPedido.Pago;
        TransacaoId = transacaoId;
    }

    public Result Cancelar(string motivo)
    {
        if (Status == StatusPedido.Enviado)
            return Result.Failure("Pedido enviado não pode ser cancelado");
        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure("Motivo é obrigatório");

        Status = StatusPedido.Cancelado;
        return Result.Success();
    }

    private void RecalcularTotal() =>
        Total = _itens.Sum(i => i.Preco * i.Quantidade);
}
```

> **Nota sobre `Result` no Domain:** se você usa o `Result` no Domain (como em `Cancelar`), ele precisa estar disponível no projeto Domain. Mantenha uma cópia mínima de `Result` no Domain (sem dependência de infra) ou um tipo de retorno próprio do Domain. O `Result` da API pode ser o mesmo tipo se você colocá-lo em um lugar acessível aos dois — mas cuidado para não puxar nada de infra junto. Recomendação: um `Result` puro no Domain, reusado pela API.

### Domain Services

Lógica de negócio que não pertence naturalmente a uma única entidade, ou que coordena várias.

```csharp
// MinhaApp.Domain/Modulos/Checkout/PoliticaDesconto.cs
namespace MinhaApp.Domain.Modulos.Checkout;

public class PoliticaDesconto
{
    public decimal CalcularDesconto(Pedido pedido, Cliente cliente)
    {
        var desconto = 0m;
        if (cliente.EhVip) desconto += pedido.Total * 0.10m;
        if (pedido.Itens.Count >= 10) desconto += pedido.Total * 0.05m;
        return Math.Min(desconto, pedido.Total * 0.20m); // teto de 20%
    }
}
```

Domain Services são instanciados pela Ação ou injetados via DI (registrados como serviço). Eles operam sobre entidades do Domain e não conhecem infraestrutura.

---

## 13. Independência entre fatias

Cada Ação é uma fatia vertical independente. As regras de isolamento:

### Uma Ação nunca injeta outra Ação

```csharp
// ❌ PROIBIDO
public class ProcessarPedido(AppDbContext db, BuscarProdutos outraAcao) { }
```

### Se uma Ação precisa de dados de outro contexto, consulta o banco direto

```csharp
// ✅ ProcessarPedido (módulo Checkout) precisa de dados de Cliente (módulo Clientes)
public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<ProcessarPedidoResponse>> Execute(
        ProcessarPedidoRequest request, CancellationToken ct)
    {
        // consulta direta ao banco, cruzando módulos no nível de dados
        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId, ct);
        // ...
    }
}
```

### Lógica complexa compartilhada vira Domain Service

Se duas Ações precisam da mesma regra (ex: cálculo de frete, política de desconto), essa regra vai para um Domain Service em `MinhaApp.Domain`, e ambas as Ações o utilizam.

**O isolamento entre módulos é lógico/organizacional, não físico no acesso a dados.** Duas Ações de módulos diferentes podem ler as mesmas tabelas. O que segura o acoplamento é a combinação de "Ação não chama Ação" + "regra compartilhada vira Domain Service".

---

## 14. Estratégia de testes

### Testes de Unidade — Domain

Focam nas entidades ricas e lógicas puras do projeto `MinhaApp.Domain`. Sem mocks, sem banco, sem framework.

```csharp
public class PedidoTests
{
    [Fact]
    public void Cancelar_PedidoEnviado_DeveFalhar()
    {
        var pedido = Pedido.Criar(Guid.NewGuid(), [new ItemPedido(...)]);
        pedido.ConfirmarPagamento("tx-123");
        // simular envio via método de domínio...

        var resultado = pedido.Cancelar("desistência");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("não pode ser cancelado");
    }
}
```

### Testes de Integração — Ações na API

Classes de Ação são testadas via Testes de Integração usando `WebApplicationFactory` e `Testcontainers` (subindo banco real em Docker). Sem mock de `DbContext` — banco real.

```csharp
public class ProcessarPedidoTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProcessarPedidoTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task ProcessarPedido_ComItensValidos_DeveRetornar200()
    {
        var request = new { Itens = new[] { new { ProdutoId = ..., Quantidade = 2 } }, FormaPagamento = "pix" };

        var response = await _client.PostAsJsonAsync("/checkout/pedidos", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

```csharp
// ApiFactory com Testcontainers
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder().Build();

    public async Task InitializeAsync() => await _db.StartAsync();
    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // substituir connection string pela do container
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(_db.GetConnectionString()));
        });
    }
}
```

**Por que integração e não unit para Ações:** como a Ação acessa `DbContext`/Dapper direto (sem Repository para mockar), testá-la isoladamente exigiria mockar o EF — frágil e pouco fiel. Testcontainers sobe um banco real, o teste exercita a query de verdade. Mais lento, muito mais confiável.

---

## 15. SOLID na LMA

- **SRP** — cada Ação faz uma coisa. Endpoint só roteia e resolve web. Domain Service tem uma responsabilidade de negócio.
- **OCP** — funcionalidade nova = Ação nova (pasta nova). Não se mexe em Ação existente.
- **LSP** — entra em gateways/policies polimórficos (`IPagamentoGateway` com PagarMe, Stripe).
- **ISP** — interfaces de infraestrutura são pequenas e específicas (`IPagamentoGateway`, `IArmazenamentoImagens`), nunca genéricas.
- **DIP** — aplicado na fronteira **Ação ↔ integrações externas** (gateways, storage, e-mail). **Não** aplicado a persistência — decisão consciente da LMA, já que banco não troca.

A inversão de dependência na LMA é seletiva: existe onde a volatilidade é real (integrações externas), não onde é teórica (banco).

---

## 16. Otimizada para IA

Boa parte do código .NET hoje é escrita ou assistida por LLMs — Claude Code, Cursor, Copilot, agentes especializados. A LMA foi desenhada considerando isso. Cada decisão da arquitetura tem dupla justificativa: simplifica para o desenvolvedor humano **e** reduz a superfície de erro quando uma IA está gerando código.

### O problema das arquiteturas tradicionais com IA

Clean Architecture, DDD ortodoxo e arquiteturas similares foram pensadas para tornar humanos mais produtivos navegando código complexo. Em uma era de geração assistida por IA, elas têm três fricções:

1. **Muitas decisões abertas.** "Esse caso é Use Case ou Service?" "Esse repositório fica no Domain ou Application?" "Esse handler usa MediatR ou injeção direta?" Cada decisão é um ponto onde a IA pode inventar uma resposta diferente entre features, gerando inconsistência.

2. **Estrutura dispersa.** Uma feature toca de 8 a 15 arquivos em 4 projetos diferentes. A IA precisa carregar muito contexto para gerar uma feature, e mesmo carregando, frequentemente erra o caminho de algum arquivo.

3. **Vocabulário variável.** `Handle`, `Execute`, `Process`, `Run`, `Apply` — o método público varia por convenção do time, e a IA pula entre estilos. Igual para nomes de classe: `XxxHandler`, `XxxService`, `XxxUseCase`, `XxxCommand`.

### Como a LMA resolve

**Estrutura previsível e rasa.** Toda Ação vive em `Modulos/{Modulo}/{Acao}/` com 3 a 4 arquivos: `Acao.cs`, `Request.cs`, `Response.cs`, `Validator.cs`. A IA sabe exatamente onde criar e onde encontrar. Sem subpastas, sem dispersão.

**Vocabulário fixo.**
- Classe = nome do verbo de negócio (`ProcessarPedido`, `BuscarProdutos`). Sem sufixo `Handler`, `Service`, `UseCase`.
- Método público único: **`Execute(request, ct)`**. Sempre.
- Retorno: **`Result<TResponse>`**. Sempre.
- A IA não tem o que inventar.

**Decisões já tomadas.** A LMA não pergunta "use Repository ou DbContext?", "MediatR ou DI direto?", "Validator manual ou IEndpointFilter?". Cada uma dessas tem uma resposta única — e essa resposta minimiza arquivos, abstrações e ambiguidade.

**Guardrails do compilador.** A separação física `MinhaApp.Domain` e `MinhaApp.Api` faz com que qualquer tentativa da IA de importar EF no Domain **não compile**. Não é "code review pega depois" — é erro de build imediato. Esse é o tipo de constraint que torna IA confiável: o que ela não pode fazer, ela não consegue fazer.

**Roteamento centralizado por módulo.** Quando a IA precisa expor uma Ação nova como endpoint, sabe exatamente em qual arquivo editar: `Endpoints/{Modulo}Endpoints.cs`. Sem caçar onde está o `Map(...)` da feature anterior.

**Validação automática global.** A IA não precisa lembrar de validar dentro da Ação, nem de adicionar `if (!ModelState.IsValid)` em todo endpoint. O filtro global faz isso. Menos código gerado, menos chance de esquecer.

**Acesso a dados direto.** Sem inventar `IPedidoRepository`, `IPedidoWriter`, `IPedidoReader` cada vez. A Ação injeta `AppDbContext` ou `IDbConnectionFactory` direto. Um padrão, uma linha no construtor.

### O que isso entrega na prática

Em projetos LMA, uma IA bem instruída (via CLAUDE.md, skill ou plugin) consegue:

- Criar uma Ação completa em uma única passagem, com Endpoint registrado, sem precisar revisitar arquivos
- Manter consistência entre features sem desvio progressivo (não inventa "umas com Repository, outras sem")
- Compilar de primeira na maioria dos casos, porque os guardrails estruturais foram respeitados
- Não introduzir dependências proibidas (MediatR, AutoMapper, IRepository genérico) porque a estrutura impede

### Como instruir IA para gerar código LMA

Três artefatos cobrem 95% dos casos:

1. **`CLAUDE.md` enxuto no root do projeto** — regras invioláveis em 50-80 linhas
2. **Templates de Ação** referenciados pelo CLAUDE.md — formato exato dos arquivos
3. **Skill ou plugin** (Claude Code) — automação real do ato de criar Ações, com entrevista estruturada

A Leanwork mantém esses artefatos como parte do ecossistema oficial da LMA.

### A questão filosófica

Não se trata de "arquitetura para IA substituir humanos". Trata-se de reconhecer que o **par humano + IA** é a unidade real de produção de código hoje, e que arquiteturas otimizadas só para uma das partes desperdiçam capacidade da outra. A LMA assume os dois.

## 17. Anti-padrões

### 17.1 Ação injetando outra Ação

```csharp
// ❌
public class ProcessarPedido(AppDbContext db, CalcularFrete outraAcao) { }
```
**Correção:** consulta direta ao banco, ou mover lógica compartilhada para Domain Service.

### 17.2 Domain referenciando infraestrutura

```csharp
// ❌ em MinhaApp.Domain
using Microsoft.EntityFrameworkCore;
```
**Correção:** não compila se os projetos estiverem separados corretamente. Domain é puro. Se precisa de EF, a lógica está no projeto errado.

### 17.3 Protocolo web vazando para a Ação

```csharp
// ❌
public class ProcessarPedido(AppDbContext db, IHttpContextAccessor http) { }
```
**Correção:** extrair o dado no Endpoint, passar limpo no Request.

### 17.4 Reintroduzir Repository

```csharp
// ❌
public interface IPedidoRepository { }
```
**Correção:** LMA não usa Repository. Acesso direto via `AppDbContext`/Dapper na Ação. (Integrações externas continuam com interface — isso não é Repository.)

### 17.5 Validação manual dentro da Ação

```csharp
// ❌
public async Task<Result<...>> Execute(Request req, CancellationToken ct)
{
    if (string.IsNullOrEmpty(req.Nome)) return Result.Failure(...);  // já era feito no filtro!
}
```
**Correção:** validação de formato fica no Validator (roda no IEndpointFilter). Na Ação, só regra de negócio (que vive no Domain).

### 17.6 Ação declarando a própria rota

```csharp
// ❌ a Ação não conhece HTTP
public class ProcessarPedido
{
    public static void Map(IEndpointRouteBuilder app) => app.MapPost(...);
}
```
**Correção:** roteamento centralizado em `Endpoints/{Modulo}Endpoints.cs`.

### 17.7 Commit implícito ou esquecido

```csharp
// ❌ esqueceu de salvar
db.Pedidos.Add(pedido);
return Result.Success(...);   // nada foi persistido!
```
**Correção:** `await db.SaveChangesAsync(ct)` explícito na última linha transacional da Ação de escrita.

### 17.8 Abstrair persistência "por via das dúvidas"

```csharp
// ❌ criar IDbContext, IUnitOfWork genérico, etc. para "desacoplar o EF"
```
**Correção:** LMA assume que o banco não troca. Acesso direto. A abstração que paga aluguel é a de integração externa, não a de banco.

---

## 18. Quando NÃO usar LMA

LMA serve a uma gama ampla de projetos API-first .NET — do MVP ao sistema complexo de longa vida, com ou sem múltiplos bancos. Mesmo assim, há casos em que outras arquiteturas pagam melhor.

**Considere outra arquitetura quando:**

- O sistema precisa **trocar de mecanismo de persistência por agregado dentro do mesmo módulo** (ex: migrar uma entidade de relacional para document store mantendo as outras como estão) — aí a abstração de Repository volta a pagar aluguel. Em LMA, troca de banco é por módulo inteiro, não por agregado.
- O domínio tem **complexidade combinatória extrema** que exige camada de aplicação dedicada e separada (motores de regra de seguros, tributação combinatória, cálculo atuarial complexo) — DDD ortodoxo com camadas explícitas pode pagar aluguel aqui.
- O sistema roda em **múltiplos runtimes/UIs compartilhando a mesma lógica de aplicação**, não só de domínio (web + desktop + CLI + mobile com mesma orquestração) — arquitetura com Application layer separada serve melhor.
- Você precisa de **rastreabilidade extrema entre regra de negócio e código** (ambientes altamente regulados onde cada regra precisa ter um arquivo nominalmente identificável e versionável separadamente) — arquiteturas mais formais ajudam.

**LMA não é a melhor opção também quando:**

- O time tem aversão estrutural a acesso direto a `DbContext` na camada de aplicação (questão cultural legítima — não vale a pena impor)
- O projeto será integrado a um codebase existente que segue Clean Architecture rigorosa — mistura de estilos cria confusão

**LMA continua sendo boa opção quando:**

- Você tem múltiplos bancos por módulo (Postgres + Mongo + ClickHouse, etc.) — isso é primeira classe na LMA
- Você tem um sistema grande com muitos módulos e times — modularidade segura a complexidade
- Você espera evolução longa — o esqueleto sustenta crescimento sem virar bagunça
- IA é parte ativa do processo de geração de código — otimizações estruturais ajudam (ver §16)
- Você quer MVP rápido sem perder qualidade arquitetural — pasta plana e zero cerimônia entregam isso

---

## 19. FAQ

**P: Sem Repository, como testo uma Ação isoladamente?**
R: Você não testa isoladamente — testa via integração com `WebApplicationFactory` + Testcontainers. A lógica pura testável isoladamente está no Domain (entidades, Domain Services), e essa sim tem teste unitário sem mocks.

**P: Posso usar EF e Dapper no mesmo projeto?**
R: Sim, e é encorajado. EF para escrita e leitura com produtividade; Dapper para leituras de alta performance (relatórios, listagens pesadas). A Ação injeta o que precisar.

**P: Posso ter múltiplos bancos por módulo?**
R: Sim — esse é um dos casos onde LMA brilha. Cada módulo pode ter seu próprio `DbContext` ou conexão apontando para o banco mais adequado ao seu workload. Exemplo: módulo `Catalogo` injeta `CatalogoDbContext` (Postgres, dados transacionais), módulo `Auditoria` injeta `IAuditoriaConnectionFactory` (Mongo, append-only flexível), módulo `Analytics` injeta `IAnalyticsConnection` (ClickHouse, colunar para agregações). Cada Ação injeta o contexto/conexão do seu módulo, e o registro no DI define qual banco cada um aponta. **Importante:** quando módulos compartilham banco, a regra de "Ação não chama Ação + lógica compartilhada via Domain Service" continua segurando o acoplamento. Quando bancos são diferentes, a separação é ainda mais natural — não há como uma Ação do Analytics, por engano, escrever em tabela do Catálogo.

**P: Como faço uma transação que envolve gateway de pagamento?**
R: Use `db.Database.BeginTransactionAsync(ct)` explicitamente na Ação. Persista o pedido como pendente, chame o gateway, e conforme o resultado faça commit ou rollback. O controle transacional fino é responsabilidade da Ação.

**P: O `Result` é o mesmo no Domain e na API?**
R: Recomendo um `Result` puro no Domain (sem nenhuma dependência de infra), reutilizado pela API. Assim a entidade rica pode retornar `Result` em métodos como `Cancelar()` sem o Domain depender de nada da API.

**P: Onde fica a lógica de "extrair ClienteId do JWT"?**
R: Estritamente no Endpoint. A Ação recebe `ClienteId` já limpo no Request.

**P: Como dois módulos compartilham uma regra de negócio?**
R: Via Domain Service em `MinhaApp.Domain`. Ambas as Ações injetam/usam o Domain Service. Ações nunca se chamam diretamente.

**P: O Validator roda dentro da Ação?**
R: Não. Roda automaticamente no `IEndpointFilter` aplicado ao grupo de endpoints, antes da Ação. Se falhar, retorna 400 e a Ação nem é chamada.

**P: Como registro as Ações no DI?**
R: Uma linha por Ação (`AddScoped<ProcessarPedido>()`) ou via scan de assembly (registrar todas as classes do namespace `Modulos`). Para projeto grande, o scan reduz boilerplate.

**P: LMA serve para MVP rápido ou só para projetos grandes?**
R: Os dois. Para MVP, você começa com `MinhaApp.Domain` quase vazio e poucos módulos na `MinhaApp.Api`. A estrutura é a mesma de um projeto grande, só com menos arquivos. Não há "versão simplificada da LMA para projetos pequenos" — a LMA já é simples.

**P: Por que dois projetos físicos desde o dia 1 e não começar com um e separar depois?**
R: Separar depois é caro e ninguém faz. Começar com dois deixa o compilador como guardião desde a primeira linha, e o custo inicial é zero (um `dotnet new classlib` a mais). Para IA gerando código, essa barreira física previne 80% dos erros de dependência.

## 20. Glossário

- **Ação** — Classe que executa um caso de uso (vertical slice). Vive em `MinhaApp.Api/Modulos/{Modulo}/{Acao}/`. Método público único `Execute(request, ct)`, retorna `Result<TResponse>`. Injeta `AppDbContext`/Dapper e gateways diretamente. Substitui Handler e Reader.
- **Endpoint (centralizado)** — Arquivo em `Endpoints/{Modulo}Endpoints.cs` que mapeia todas as rotas de um módulo via extension method de Minimal API, resolve protocolo web e direciona para Ações.
- **Módulo** — Bounded context de negócio (Checkout, Catálogo, Clientes). Existe tanto no Domain (entidades) quanto na API (Ações e Endpoints).
- **Domain Service** — Lógica de negócio pura que não pertence a uma única entidade ou coordena várias. Vive em `MinhaApp.Domain`.
- **Result Pattern** — `Result<T>` indicando sucesso/falha de negócio. Retornado por toda Ação e por métodos de domínio que podem falhar.
- **IEndpointFilter (ValidationFilter)** — Filtro global que executa o Validator do Request automaticamente antes da Ação, retornando 400 em caso de falha.
- **Commit explícito** — `SaveChangesAsync` chamado pela própria Ação de escrita, na última linha transacional.
- **Gateway** — Interface de integração externa (pagamento, storage, e-mail) definida e implementada na camada de Infrastructure da API. Mantém DIP onde a volatilidade é real.

---

**Fim do documento.**
