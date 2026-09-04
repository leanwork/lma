# LMA — Templates de Ação

> Templates copiáveis para criar Ações no padrão LMA v1.0.
> Documento principal: `docs/architecture/lma-v1.0.md`
> Resumo no root: `CLAUDE.md`

## Como usar

1. Identifique o **tipo da Ação**:
   - **Leitura simples (EF Core)** → [Template A](#template-a--leitura-com-ef-core)
   - **Leitura de alta performance (Dapper)** → [Template B](#template-b--leitura-com-dapper)
   - **Escrita CRUD** (entidade anêmica, sem regras) → [Template C](#template-c--escrita-crud)
   - **Escrita com regra de negócio** (entidade rica) → [Template D](#template-d--escrita-com-regra)
   - **Escrita com integração externa** (gateway, transação distribuída) → [Template E](#template-e--escrita-com-gateway)

2. Crie a pasta `MinhaApp.Api/Modulos/{Modulo}/{NomeAcao}/`.

3. Copie os arquivos do template e substitua placeholders:
   - `{Modulo}` — bounded context (`Catalogo`, `Checkout`, `Clientes`)
   - `{Acao}` — nome da Ação no verbo de negócio (`ProcessarPedido`, `BuscarProdutos`)
   - `{Entidade}` — entidade do domínio (`Pedido`, `Produto`)

4. Registre no DI (`Program.cs` ou extension method) e adicione a rota no `Endpoints/{Modulo}Endpoints.cs`.

---

## Template A — Leitura com EF Core

**Quando usar:** consultas comuns, listagens com filtros, busca por ID, paginação. Quando produtividade importa mais do que performance extrema.

### `{Acao}.cs`

```csharp
// MinhaApp.Api/Modulos/{Modulo}/{Acao}/{Acao}.cs
using MinhaApp.Api.Common;
using MinhaApp.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}(AppDbContext db)
{
    public async Task<Result<{Acao}Response>> Execute(
        {Acao}Request request,
        CancellationToken ct)
    {
        var query = db.{Entidade}s.AsNoTracking();

        // filtros condicionais
        if (!string.IsNullOrWhiteSpace(request.Termo))
            query = query.Where(x => x.Nome.Contains(request.Termo));

        if (request.CategoriaId is not null)
            query = query.Where(x => x.CategoriaId == request.CategoriaId);

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(x => new {Acao}Item(x.Id, x.Nome, x.Preco))
            .ToListAsync(ct);

        return Result.Success(new {Acao}Response(itens, total, request.Pagina, request.Tamanho));
    }
}
```

### `{Acao}Request.cs`

```csharp
namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public record {Acao}Request(
    string? Termo = null,
    Guid? CategoriaId = null,
    int Pagina = 1,
    int Tamanho = 20);
```

### `{Acao}Response.cs`

```csharp
namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public record {Acao}Response(
    List<{Acao}Item> Itens,
    int Total,
    int Pagina,
    int Tamanho);

public record {Acao}Item(
    Guid Id,
    string Nome,
    decimal Preco);
```

### `{Acao}Validator.cs`

```csharp
using FluentValidation;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}Validator : AbstractValidator<{Acao}Request>
{
    public {Acao}Validator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.Tamanho).InclusiveBetween(1, 100);
        RuleFor(x => x.Termo).MaximumLength(200);
    }
}
```

### Checklist do Template A

- [ ] Usa `AsNoTracking()` em todas as queries
- [ ] Projeta via `Select(...)` para o tipo Response (não retorna entidade do Domain)
- [ ] Não chama `SaveChangesAsync` (leitura não muda estado)
- [ ] Filtros condicionais usando `if (... is not null) query = query.Where(...)`
- [ ] Paginação com `Skip` + `Take` quando aplicável

---

## Template B — Leitura com Dapper

**Quando usar:** relatórios, agregações pesadas, listagens com SQL complexo, qualquer cenário onde performance é crítica e EF Core seria overhead.

### `{Acao}.cs`

```csharp
// MinhaApp.Api/Modulos/{Modulo}/{Acao}/{Acao}.cs
using MinhaApp.Api.Common;
using MinhaApp.Api.Infrastructure.Database;
using Dapper;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}(IDbConnectionFactory connectionFactory)
{
    public async Task<Result<{Acao}Response>> Execute(
        {Acao}Request request,
        CancellationToken ct)
    {
        using var conn = await connectionFactory.CreateAsync(ct);

        const string sql = """
            SELECT
                p.CategoriaId,
                c.Nome AS CategoriaNome,
                COUNT(*) AS QuantidadePedidos,
                SUM(p.Total) AS Faturamento
            FROM Pedidos p
            INNER JOIN Categorias c ON c.Id = p.CategoriaId
            WHERE p.CriadoEm BETWEEN @De AND @Ate
              AND p.Status = 'Pago'
            GROUP BY p.CategoriaId, c.Nome
            ORDER BY Faturamento DESC
            """;

        var linhas = await conn.QueryAsync<{Acao}Item>(
            new CommandDefinition(sql,
                new { request.De, request.Ate },
                cancellationToken: ct));

        return Result.Success(new {Acao}Response(linhas.ToList()));
    }
}
```

### `{Acao}Request.cs`

```csharp
public record {Acao}Request(DateTime De, DateTime Ate);
```

### `{Acao}Response.cs`

```csharp
public record {Acao}Response(List<{Acao}Item> Linhas);

public record {Acao}Item(
    Guid CategoriaId,
    string CategoriaNome,
    int QuantidadePedidos,
    decimal Faturamento);
```

### `{Acao}Validator.cs`

```csharp
public class {Acao}Validator : AbstractValidator<{Acao}Request>
{
    public {Acao}Validator()
    {
        RuleFor(x => x.De).LessThan(x => x.Ate)
            .WithMessage("Data inicial deve ser anterior à final");
        RuleFor(x => x.Ate).LessThanOrEqualTo(DateTime.UtcNow);
    }
}
```

### Checklist do Template B

- [ ] Usa `IDbConnectionFactory` (não `AppDbContext`)
- [ ] SQL parametrizado (`@parametro`), nunca interpolação de string
- [ ] `CommandDefinition` com `cancellationToken`
- [ ] DTO de leitura específico da Ação
- [ ] `using` no `IDbConnection` para garantir dispose

---

## Template C — Escrita CRUD

**Quando usar:** criar/atualizar entidades anêmicas (Cliente, Categoria, Tag, Endereço) sem regras de transição de estado.

### `{Acao}.cs`

```csharp
// MinhaApp.Api/Modulos/{Modulo}/{Acao}/{Acao}.cs
using MinhaApp.Api.Common;
using MinhaApp.Api.Infrastructure.Database;
using MinhaApp.Domain.Modulos.{Modulo};
using Microsoft.EntityFrameworkCore;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}(AppDbContext db)
{
    public async Task<Result<{Acao}Response>> Execute(
        {Acao}Request request,
        CancellationToken ct)
    {
        var entidade = new {Entidade}
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            CriadoEm = DateTime.UtcNow
        };

        db.{Entidade}s.Add(entidade);
        await db.SaveChangesAsync(ct);   // commit explícito

        return Result.Success(new {Acao}Response(entidade.Id));
    }
}
```

### Variante: Atualizar (busca, modifica, salva)

```csharp
public class Atualizar{Entidade}(AppDbContext db)
{
    public async Task<Result> Execute(
        Atualizar{Entidade}Request request,
        CancellationToken ct)
    {
        var entidade = await db.{Entidade}s
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entidade is null)
            return Result.Failure("{Entidade} não encontrado");

        entidade.Nome = request.Nome;
        entidade.Email = request.Email;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### `{Acao}Request.cs`, `{Acao}Response.cs`, `{Acao}Validator.cs`

```csharp
public record {Acao}Request(string Nome, string Email);

public record {Acao}Response(Guid Id);

public class {Acao}Validator : AbstractValidator<{Acao}Request>
{
    public {Acao}Validator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

### Checklist do Template C

- [ ] Entidade anêmica (setters públicos, sem método de comportamento)
- [ ] Atribuições simples (sem `if/else` decidindo mutação)
- [ ] `SaveChangesAsync(ct)` explícito na última linha
- [ ] Sem regra de negócio (se aparecer regra, vire Template D)

---

## Template D — Escrita com Regra

**Quando usar:** transições de estado em entidades ricas (cancelar pedido, aprovar fatura, publicar artigo, mudar status de assinatura). Vocabulário ubíquo é evidente.

### `{Acao}.cs`

```csharp
// MinhaApp.Api/Modulos/{Modulo}/{Acao}/{Acao}.cs
using MinhaApp.Api.Common;
using MinhaApp.Api.Infrastructure.Database;
using MinhaApp.Domain.Modulos.{Modulo};
using Microsoft.EntityFrameworkCore;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}(AppDbContext db)
{
    public async Task<Result> Execute(
        {Acao}Request request,
        CancellationToken ct)
    {
        var {entidade} = await db.{Entidade}s
            .FirstOrDefaultAsync(x => x.Id == request.{Entidade}Id, ct);

        if ({entidade} is null)
            return Result.Failure("{Entidade} não encontrado");

        // regra de negócio vive AQUI, no método do Domain rico
        var resultado = {entidade}.{MetodoDeDominio}(request.Motivo);
        if (resultado.IsFailure)
            return resultado;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### Método de domínio em `MinhaApp.Domain/Modulos/{Modulo}/{Entidade}.cs`

```csharp
namespace MinhaApp.Domain.Modulos.{Modulo};

public class {Entidade}
{
    public Guid Id { get; private set; }
    public Status{Entidade} Status { get; private set; }
    // ... outras propriedades com setter privado

    private {Entidade}() { }

    public static {Entidade} Criar(/* parâmetros */)
    {
        return new {Entidade}
        {
            Id = Guid.NewGuid(),
            Status = Status{Entidade}.Inicial
        };
    }

    public Result {MetodoDeDominio}(string motivo)
    {
        // 1. Pré-condições (invariantes)
        if (Status == Status{Entidade}.Finalizado)
            return Result.Failure("{Entidade} finalizado não pode ser modificado");

        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure("Motivo é obrigatório");

        // 2. Mudança de estado
        Status = Status{Entidade}.Cancelado;

        return Result.Success();
    }
}
```

### `{Acao}Request.cs`, `{Acao}Validator.cs`

```csharp
public record {Acao}Request(string Motivo)
{
    // ID vem da rota, preenchido pelo Endpoint
    public Guid {Entidade}Id { get; init; }
}

public class {Acao}Validator : AbstractValidator<{Acao}Request>
{
    public {Acao}Validator()
    {
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
    }
}
```

### Checklist do Template D

- [ ] Entidade rica com setters privados, construtor privado, factory `Criar(...)`
- [ ] Método de domínio com nome ubíquo (`Cancelar`, `Aprovar`) — **não** `SetStatus`, `UpdateXxx`
- [ ] Regra de negócio **no método de domínio**, não na Ação
- [ ] Ação apenas orquestra: busca, chama método, persiste
- [ ] Result Pattern em ambos: domínio e Ação
- [ ] Domain **não** importa nada de infra (compilador valida)

---

## Template E — Escrita com Gateway

**Quando usar:** Ações que orquestram operação interna + chamada a serviço externo (gateway de pagamento, envio de e-mail crítico, integração com terceiro). Geralmente envolve transação explícita.

### `{Acao}.cs`

```csharp
// MinhaApp.Api/Modulos/{Modulo}/{Acao}/{Acao}.cs
using MinhaApp.Api.Common;
using MinhaApp.Api.Infrastructure.Database;
using MinhaApp.Api.Infrastructure.Gateways;
using MinhaApp.Domain.Modulos.{Modulo};
using Microsoft.EntityFrameworkCore;

namespace MinhaApp.Api.Modulos.{Modulo}.{Acao};

public class {Acao}(
    AppDbContext db,
    I{Servico}Gateway gateway)
{
    public async Task<Result<{Acao}Response>> Execute(
        {Acao}Request request,
        CancellationToken ct)
    {
        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.Id == request.ClienteId, ct);
        if (cliente is null)
            return Result.Failure<{Acao}Response>("Cliente não encontrado");

        // 1. Construir agregado em estado pendente
        var pedido = Pedido.Criar(request.ClienteId, request.Itens);
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);   // persiste como pendente

        // 2. Chamar gateway externo
        var cobranca = await gateway.CobrarAsync(pedido.Total, ct);
        if (cobranca.IsFailure)
        {
            pedido.MarcarFalhaPagamento(cobranca.Error);
            await db.SaveChangesAsync(ct);
            return Result.Failure<{Acao}Response>(cobranca.Error);
        }

        // 3. Confirmar e persistir estado final
        pedido.ConfirmarPagamento(cobranca.TransacaoId);
        await db.SaveChangesAsync(ct);

        return Result.Success(new {Acao}Response(pedido.Id, pedido.Total, cobranca.TransacaoId));
    }
}
```

### Interface do gateway em `MinhaApp.Api/Infrastructure/Gateways/`

```csharp
namespace MinhaApp.Api.Infrastructure.Gateways;

public interface I{Servico}Gateway
{
    Task<ResultadoCobranca> CobrarAsync(decimal valor, CancellationToken ct);
}

public record ResultadoCobranca(bool IsSuccess, string? TransacaoId, string? Error)
{
    public bool IsFailure => !IsSuccess;
    public static ResultadoCobranca Sucesso(string transacaoId) => new(true, transacaoId, null);
    public static ResultadoCobranca Falha(string erro) => new(false, null, erro);
}
```

### Implementação concreta (mesmo arquivo ou separado)

```csharp
public class PagarMeGateway(HttpClient http, IConfiguration config) : I{Servico}Gateway
{
    public async Task<ResultadoCobranca> CobrarAsync(decimal valor, CancellationToken ct)
    {
        // implementação concreta da chamada HTTP ao PagarMe
        // ...
    }
}
```

### Checklist do Template E

- [ ] Gateway é interface em `Infrastructure/Gateways/`, implementação na mesma pasta
- [ ] Ação trata caso de falha do gateway atualizando o estado do agregado
- [ ] Estado intermediário (pendente) é persistido antes da chamada externa — auditoria e idempotência
- [ ] Se há rollback, considerar `db.Database.BeginTransactionAsync(ct)` explícito
- [ ] Gateway injetado por interface (DIP onde paga aluguel)

---

## Composition Root — Registro no DI

Para projeto pequeno, registre cada Ação no `Program.cs`:

```csharp
builder.Services.AddScoped<ProcessarPedido>();
builder.Services.AddScoped<CancelarPedido>();
builder.Services.AddScoped<BuscarProdutos>();
```

Para projeto grande, use scan de assembly:

```csharp
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.InNamespaces("MinhaApp.Api.Modulos"))
    .AsSelf()
    .WithScopedLifetime());
```

Gateways e infra registrados pela interface:

```csharp
builder.Services.AddScoped<IPagamentoGateway, PagarMeGateway>();
builder.Services.AddScoped<IArmazenamentoImagens, AzureBlobStorage>();
```

---

## Roteamento — `Endpoints/{Modulo}Endpoints.cs`

Sempre que criar uma Ação nova, adicione a rota no arquivo de endpoints do módulo:

```csharp
public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/checkout")
            .WithTags("Checkout")
            .AddEndpointFilter<ValidationFilter>()
            .RequireAuthorization();

        grupo.MapPost("/pedidos", async (
            ProcessarPedidoRequest body,
            ClaimsPrincipal user,                       // protocolo web AQUI
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

        // outras rotas do módulo...

        return app;
    }
}
```

E chame no `Program.cs`:

```csharp
app.MapCheckoutEndpoints();
```

---

## Como escolher entre os templates

| Cenário | Template |
|---------|----------|
| Listar/buscar com filtros, paginação, sem performance crítica | **A** (EF) |
| Relatório, agregação, listagem com SQL complexo | **B** (Dapper) |
| Criar Cliente, Endereço, Tag, Categoria | **C** (CRUD anêmico) |
| Cancelar Pedido, Aprovar Fatura, Publicar Artigo | **D** (regra rica) |
| Processar pagamento, enviar e-mail crítico, integrar com terceiro | **E** (gateway) |

Em dúvida entre **C** e **D**: a entidade pode entrar em estado inválido por uma única atribuição? Se sim → **D**. Se não → **C**.
