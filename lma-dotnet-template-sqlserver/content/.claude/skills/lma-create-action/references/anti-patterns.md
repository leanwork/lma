# LMA — Anti-padrões para evitar ao gerar Ação

> Lista dos erros mais comuns ao gerar código LMA. Consulte antes de cada geração e use como filtro de auto-revisão.

## 1. Ação com sufixo de framework antigo

```csharp
// ❌ ERRADO
public class CancelarPedidoHandler { }
public class CancelarPedidoService { }
public class CancelarPedidoUseCase { }
public class CancelarPedidoCommand { }
public class CancelarPedidoQuery { }
```

```csharp
// ✅ CERTO — nome é o verbo de negócio, sem sufixo
public class CancelarPedido { }
public class BuscarProdutos { }
public class ProcessarPedido { }
```

**Por que importa:** o nome da classe é o vocabulário ubíquo. Sufixos técnicos diluem o significado e são herança cultural de frameworks que a LMA abandona.

## 2. Endpoint dentro da Ação

```csharp
// ❌ ERRADO — Ação conhece HTTP
public class ProcessarPedido
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/pedidos", /* ... */);

    public async Task<Result<...>> Execute(...) { ... }
}
```

```csharp
// ✅ CERTO — Ação só executa, roteamento em arquivo separado
public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<...>> Execute(...) { ... }
}

// Endpoints/CheckoutEndpoints.cs
public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(...)
    {
        grupo.MapPost("/pedidos", async (... ProcessarPedido acao ...) =>
            (await acao.Execute(req, ct)).ToHttp());
        return app;
    }
}
```

**Por que importa:** LMA centraliza roteamento por módulo. Ação não conhece HTTP — é testável sem WebApplicationFactory para lógica pura.

## 3. Ação injetando HttpContext, ClaimsPrincipal ou Request HTTP

```csharp
// ❌ ERRADO
public class ProcessarPedido(
    AppDbContext db,
    IHttpContextAccessor http,        // ← protocolo web na Ação!
    ClaimsPrincipal user)             // ← protocolo web na Ação!
{
    public async Task<Result<...>> Execute(ProcessarPedidoRequest req, CancellationToken ct)
    {
        var clienteId = user.FindFirstValue("sub");  // ← extração JWT na Ação!
        // ...
    }
}
```

```csharp
// ✅ CERTO — extração no Endpoint, ID limpo no Request
// Endpoint:
grupo.MapPost("/pedidos", async (
    ProcessarPedidoRequest body,
    ClaimsPrincipal user,                            // ← protocolo web AQUI
    ProcessarPedido acao,
    CancellationToken ct) =>
{
    var clienteId = Guid.Parse(user.FindFirstValue("sub")!);
    var request = body with { ClienteId = clienteId };
    return (await acao.Execute(request, ct)).ToHttp();
});

// Ação:
public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<...>> Execute(ProcessarPedidoRequest req, CancellationToken ct)
    {
        // req.ClienteId já é um Guid limpo. A Ação não sabe de onde veio.
    }
}
```

## 4. Reintroduzir Repository ou Writer

```csharp
// ❌ ERRADO — LMA não usa Repository
public interface IPedidoRepository
{
    Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task SalvarAsync(Pedido pedido, CancellationToken ct);
}

public class ProcessarPedido(IPedidoRepository repo) { }
```

```csharp
// ✅ CERTO — acesso direto a DbContext na Ação
public class ProcessarPedido(AppDbContext db, IPagamentoGateway gateway)
{
    public async Task<Result<...>> Execute(...)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        // ...
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
    }
}
```

**Exceção importante:** integrações externas (gateway, storage, e-mail) **continuam com interface**. Isso não é Repository — é abstração de I/O externo, e Repository de persistência é o que LMA elimina.

## 5. Regra de negócio na Ação

```csharp
// ❌ ERRADO
public class CancelarPedido(AppDbContext db)
{
    public async Task<Result> Execute(Guid id, string motivo, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return Result.Failure("Não encontrado");

        if (pedido.Status == StatusPedido.Entregue)             // ← regra na Ação!
            return Result.Failure("Pedido entregue não pode...");

        pedido.Status = StatusPedido.Cancelado;                  // ← mutação direta!
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

```csharp
// ✅ CERTO — regra no método de domínio rico
public class CancelarPedido(AppDbContext db)
{
    public async Task<Result> Execute(Guid id, string motivo, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return Result.Failure("Não encontrado");

        var resultado = pedido.Cancelar(motivo);                 // ← chama Domain
        if (resultado.IsFailure) return resultado;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// Domain:
public Result Cancelar(string motivo)
{
    if (Status == StatusPedido.Entregue)
        return Result.Failure("Pedido entregue não pode ser cancelado");
    if (string.IsNullOrWhiteSpace(motivo))
        return Result.Failure("Motivo é obrigatório");

    Status = StatusPedido.Cancelado;
    return Result.Success();
}
```

## 6. Validação manual dentro da Ação

```csharp
// ❌ ERRADO — duplicação com o Validator
public async Task<Result<...>> Execute(CriarClienteRequest req, CancellationToken ct)
{
    if (string.IsNullOrEmpty(req.Nome)) return Result.Failure("Nome obrigatório");
    if (!req.Email.Contains("@")) return Result.Failure("Email inválido");
    // ... (FluentValidation no filtro já fez isso)
}
```

```csharp
// ✅ CERTO — Ação assume Request válido, FluentValidation cuida do formato
public async Task<Result<...>> Execute(CriarClienteRequest req, CancellationToken ct)
{
    var cliente = new Cliente { Id = Guid.NewGuid(), Nome = req.Nome, Email = req.Email };
    db.Clientes.Add(cliente);
    await db.SaveChangesAsync(ct);
    return Result.Success(new CriarClienteResponse(cliente.Id));
}

// CriarClienteValidator.cs cuida do formato
public class CriarClienteValidator : AbstractValidator<CriarClienteRequest>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

## 7. Ação esquecendo SaveChangesAsync

```csharp
// ❌ ERRADO — nada é persistido
public async Task<Result<...>> Execute(...)
{
    db.Pedidos.Add(pedido);
    return Result.Success(...);  // SaveChangesAsync? quem?
}
```

```csharp
// ✅ CERTO — commit explícito na última linha transacional
public async Task<Result<...>> Execute(...)
{
    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync(ct);
    return Result.Success(new ProcessarPedidoResponse(pedido.Id));
}
```

## 8. Ação injetando outra Ação

```csharp
// ❌ ERRADO
public class ProcessarPedido(
    AppDbContext db,
    CalcularFrete outraAcao)        // ← acoplamento entre Ações!
{ }
```

```csharp
// ✅ CERTO — lógica compartilhada vira Domain Service
public class ProcessarPedido(AppDbContext db, CalculadoraFrete calculadora)
{
    // CalculadoraFrete é Domain Service em MinhaApp.Domain/Modulos/Checkout/
}
```

## 9. Domain importando framework de infra

```csharp
// ❌ ERRADO — em MinhaApp.Domain/Modulos/Checkout/Pedido.cs
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.AspNetCore.Http;

public class Pedido { /* ... */ }
```

**Correção:** Domain é puro. Se compila, o csproj do Domain tem referência errada — remover.

## 10. Subpastas dentro da pasta da Ação

```
// ❌ ERRADO — Clean Architecture infiltrada
Modulos/Checkout/ProcessarPedido/
├── Application/
│   └── ProcessarPedidoUseCase.cs
├── Domain/
│   └── PedidoEntity.cs
└── Infrastructure/
    └── PagarMeService.cs
```

```
// ✅ CERTO — pasta plana
Modulos/Checkout/ProcessarPedido/
├── ProcessarPedido.cs
├── ProcessarPedidoRequest.cs
├── ProcessarPedidoResponse.cs
└── ProcessarPedidoValidator.cs
```

## 11. DTOs em pasta central

```
// ❌ ERRADO
MinhaApp.Api/
├── DTOs/
│   ├── PedidoDto.cs                    ← pasta global
│   ├── ClienteDto.cs
│   └── ...
└── Modulos/
```

```
// ✅ CERTO — DTOs vivem no slice da Ação
MinhaApp.Api/
└── Modulos/
    ├── Checkout/
    │   └── ProcessarPedido/
    │       ├── ProcessarPedidoRequest.cs
    │       └── ProcessarPedidoResponse.cs
    └── Catalogo/
        └── BuscarProdutos/
            └── BuscarProdutosResponse.cs
```

## 12. Uso de MediatR ou AutoMapper

```csharp
// ❌ ERRADO
public class ProcessarPedidoCommand : IRequest<Result<...>> { }
public class ProcessarPedidoHandler : IRequestHandler<ProcessarPedidoCommand, Result<...>> { }

var dto = _mapper.Map<PedidoDto>(pedido);
```

**Correção:** LMA não usa MediatR nem AutoMapper. Ações são classes normais via DI. Mapeamento é explícito.
