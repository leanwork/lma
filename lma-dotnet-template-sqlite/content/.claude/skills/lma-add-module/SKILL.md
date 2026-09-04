---
name: lma-add-module
description: Adicionar um módulo (bounded context) novo em projeto seguindo Lean Modular Architecture (LMA) v1.0. Use sempre que o usuário pedir para "criar módulo", "adicionar bounded context", "criar área de Pedidos/Clientes/Estoque/etc", "iniciar contexto novo", "criar grupo de funcionalidades". A skill cria a estrutura de pastas do módulo (MinhaApp.Api/Modulos/{Modulo}/, MinhaApp.Api/Endpoints/{Modulo}Endpoints.cs, MinhaApp.Domain/Modulos/{Modulo}/), gera o arquivo de Endpoints com extension methods Map{Modulo}Endpoints() vazio, cria a entidade raiz inicial (anêmica ou rica), configura o EF Core e atualiza Program.cs com as chamadas de registro de rota.
---

# LMA — Adicionar Módulo

Criação de bounded context novo no padrão LMA v1.0.

## Workflow

### Etapa 1: Entrevista

1. **Nome do módulo** (PascalCase, plural quando faz sentido): `Pedidos`, `Clientes`, `Catalogo`
2. **Entidade raiz inicial:** `Pedido`, `Cliente`, `Produto`
3. **Anêmica ou rica?**
4. **Banco de dados:** EF Core com qual banco? Dapper? Banco diferente do principal (múltiplos BDs)?
5. **Ações iniciais para criar junto?**

### Etapa 2: Estrutura gerada

```
MinhaApp.Domain/Modulos/{Modulo}/
└── {Entidade}.cs

MinhaApp.Api/
├── Endpoints/
│   └── {Modulo}Endpoints.cs
└── Modulos/
    └── {Modulo}/
        └── (Ações criadas pela skill lma-create-action)

Infrastructure/Database/
├── Configurations/
│   └── {Entidade}Configuration.cs
└── (AppDbContext.cs — adicionar DbSet)
```

### Etapa 3: `{Modulo}Endpoints.cs`

```csharp
// MinhaApp.Api/Endpoints/{Modulo}Endpoints.cs
using MinhaApp.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MinhaApp.Api.Endpoints;

public static class {Modulo}Endpoints
{
    public static IEndpointRouteBuilder Map{Modulo}Endpoints(
        this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/{modulo}")
            .WithTags("{Modulo}")
            .AddEndpointFilter<ValidationFilter>();
            // .RequireAuthorization(); // descomente se necessário

        // ─── Rotas serão adicionadas aqui ────────────────────────────
        // Exemplo: grupo.MapPost("/", async (...) => { ... });

        return app;
    }
}
```

### Etapa 4: Entidade no Domain

**Anêmica:**
```csharp
// MinhaApp.Domain/Modulos/{Modulo}/{Entidade}.cs
namespace MinhaApp.Domain.Modulos.{Modulo};

public class {Entidade}
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
```

**Rica:**
```csharp
namespace MinhaApp.Domain.Modulos.{Modulo};

public class {Entidade}
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private {Entidade}() { }

    public static {Entidade} Criar(string nome)
    {
        return new {Entidade}
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            CriadoEm = DateTime.UtcNow
        };
    }

    // métodos de comportamento aqui
}
```

### Etapa 5: Configuração EF e DbSet

```csharp
// Infrastructure/Database/Configurations/{Entidade}Configuration.cs
public class {Entidade}Configuration : IEntityTypeConfiguration<{Entidade}>
{
    public void Configure(EntityTypeBuilder<{Entidade}> builder)
    {
        builder.ToTable("{Entidade}s");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CriadoEm);
    }
}

// Em AppDbContext.cs — adicionar:
public DbSet<{Entidade}> {Entidade}s => Set<{Entidade}>();
```

**Múltiplos bancos:** se este módulo usa banco diferente, criar `{Modulo}DbContext` separado:

```csharp
public class {Modulo}DbContext(DbContextOptions<{Modulo}DbContext> options) : DbContext(options)
{
    public DbSet<{Entidade}> {Entidade}s => Set<{Entidade}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Modulo}DbContext).Assembly,
            t => t.Namespace!.Contains("Modulos.{Modulo}"));
    }
}
```

### Etapa 6: Program.cs

```csharp
// Registrar DbContext (ou contexto específico do módulo)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Mapear endpoints
app.Map{Modulo}Endpoints();  // ← adicionar esta linha
```

### Etapa 7: Avisos pós-criação

Lembre o usuário de:
1. Rodar migration: `dotnet ef migrations add Add{Modulo} && dotnet ef database update`
2. Criar Ações dentro do módulo com a skill `lma-create-action`
3. Se módulo é novo bounded context, verificar independência com outros módulos

## Quando NÃO criar módulo novo

- Quer adicionar 1 Ação isolada num módulo existente → usar `lma-create-action`
- Nome proposto sobrepõe módulo existente → sugerir usar o existente
- Escopo é grande demais → sugerir quebrar em módulos menores
