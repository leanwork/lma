---
description: Inicializa um projeto .NET novo no padrão LMA v1.0 (dois csproj, estrutura de pastas, AppDbContext, Result, Entity base, ValidationFilter, exemplo mínimo)
argument-hint: <nome-do-projeto> [--database sqlserver|postgres|sqlite]
---

# /lma-init — Inicializar projeto LMA

Inicializa um projeto .NET novo com dois projetos (`MinhaApp.Domain` e `MinhaApp.Api`) seguindo LMA v1.0.

## Comportamento

### 1. Validar argumento

- `$ARGUMENTS` deve ser nome em PascalCase (`MinhaApp`, `SistemaVendas`)
- Banco padrão: `sqlserver`. Aceita: `sqlserver`, `postgres`, `sqlite`

### 2. Criar estrutura

```
{Projeto}/
├── src/
│   ├── {Projeto}.Domain/
│   │   ├── {Projeto}.Domain.csproj   ← zero PackageReference de infra
│   │   └── Modulos/
│   │       └── _Common/
│   │           ├── Result.cs
│   │           └── Entity.cs
│   │
│   └── {Projeto}.Api/
│       ├── {Projeto}.Api.csproj      ← referencia Domain
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── CLAUDE.md                 ← cópia do template LMA
│       ├── Common/
│       │   ├── Result.cs             ← reexporta ou reusa do Domain
│       │   └── ValidationFilter.cs
│       ├── Endpoints/                ← pasta criada, vazia aguardando módulos
│       ├── Modulos/                  ← pasta criada, vazia aguardando Ações
│       └── Infrastructure/
│           └── Database/
│               └── AppDbContext.cs
│
├── tests/
│   └── {Projeto}.Tests/
│       └── {Projeto}.Tests.csproj
│
├── docs/
│   └── architecture/
│       ├── lma-v1.0.md
│       ├── lma-templates.md
│       └── lma-checklist-pr.md
│
├── .gitignore
└── README.md
```

### 3. Conteúdo dos arquivos base

**`{Projeto}.Domain.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**`Domain/Modulos/_Common/Result.cs`:**
```csharp
namespace {Projeto}.Domain.Modulos._Common;

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
        : base(isSuccess, error) => Value = value;
}
```

**`Api/Common/ValidationFilter.cs`:**
```csharp
using FluentValidation;

namespace {Projeto}.Api.Common;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments
            .FirstOrDefault(a => a is not null && a.GetType().Name.EndsWith("Request"));

        if (request is not null)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
            var validator = context.HttpContext.RequestServices
                .GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                var validationContext = new ValidationContext<object>(request);
                var result = await validator.ValidateAsync(validationContext);
                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());
                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
```

**`Program.cs`:**
```csharp
using {Projeto}.Api.Infrastructure.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.Use{Database}(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ─── Ações registradas aqui (ou via scan de assembly) ────────────────
// builder.Services.AddScoped<ProcessarPedido>();

// ─── Gateways e infra registrados aqui ───────────────────────────────
// builder.Services.AddScoped<IPagamentoGateway, PagarMeGateway>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseSwagger().UseSwaggerUI();

// ─── Endpoints mapeados aqui ─────────────────────────────────────────
// app.MapCheckoutEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();
```

**Pacotes NuGet por banco:**
- SQL Server: `Microsoft.EntityFrameworkCore.SqlServer`
- Postgres: `Npgsql.EntityFrameworkCore.PostgreSQL`
- SQLite: `Microsoft.EntityFrameworkCore.Sqlite`

Todos os projetos: `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore`.
Testes: `xunit`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MsSql`.

### 4. Copiar docs e CLAUDE.md

Copiar de `${CLAUDE_PLUGIN_ROOT}/docs/` para `docs/architecture/` do projeto.
Copiar `CLAUDE-template.md` para `CLAUDE.md` no root do projeto.

### 5. Output final

```
✅ Projeto LMA criado: {Projeto}

Dois projetos físicos:
  - src/{Projeto}.Domain/    (puro, zero infra)
  - src/{Projeto}.Api/       (web + infra, referencia Domain)

Próximos passos:
  1. cd {Projeto}/src/{Projeto}.Api
  2. dotnet restore && dotnet build
  3. Configurar connection string em appsettings.Development.json
  4. Criar primeiro módulo: invoke skill `lma-add-module`
  5. Criar primeira Ação: invoke skill `lma-create-action`

Documentação:
  - docs/architecture/lma-v1.0.md
  - docs/architecture/lma-templates.md
  - docs/architecture/lma-checklist-pr.md
```
