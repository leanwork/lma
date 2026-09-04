---
name: lma-refactor-to-rich
description: Refatorar entidade anêmica em entidade rica seguindo Lean Modular Architecture (LMA) v1.0. Use sempre que o usuário pedir para "tornar entidade rica", "encapsular regras na entidade", "mover regra do handler para entidade", "adicionar regra de negócio na entidade", "transformar setter público em método de comportamento", "refatorar para domínio rico", ou quando descrever cenário onde uma entidade precisa proteger invariantes de estado. A skill identifica pontos de mutação atual (setters públicos, atribuições diretas na Ação), propõe métodos de comportamento com vocabulário ubíquo, atualiza a entidade no MinhaApp.Domain (setters privados, construtor privado, factory Criar, Result pattern), refatora as Ações que mutavam a entidade diretamente, e ajusta a configuração do EF Core se necessário. Também detecta se a entidade não deveria ser rica (CRUD puro) e aconselha manter anêmica.
---

# LMA — Refatorar para Entidade Rica

Conversão de entidade anêmica para rica no projeto `MinhaApp.Domain`.

## Princípios

1. **Nem toda entidade deve ser rica.** Formulários puros (Cliente, Endereço, Categoria) ficam anêmicos. Refatore só quando há invariante real.
2. **Vocabulário ubíquo.** Método chama-se `Cancelar`, `Aprovar`, `Publicar` — nunca `SetStatus`, `UpdateXxx`.
3. **Result em vez de exception.** Falhas de regra retornam `Result.Failure(...)`.
4. **Construtor privado + factory.** Entidade rica nasce só via `Criar(...)`.
5. **Setters privados.** Toda propriedade que muta tem setter privado.
6. **Domain não conhece infra.** Nenhum `using EF`, `using MediatR` no arquivo.

## Workflow

### Etapa 1: Diagnóstico

**Deve ser rica (refatorar):**
- Ação tem `if` decidindo se mutação é permitida (`if (pedido.Status == ...)`)
- Atribuição direta com pré-condição implícita (`entidade.Status = StatusPedido.Cancelado`)
- Vocabulário ubíquo aparece em comentários ("cancelar pedido se não foi entregue")
- Regra espalhada em múltiplas Ações

**Deve ficar anêmica (não refatorar):**
- Entidade é CRUD puro (Cliente, Endereço, Categoria, Tag)
- Validações são só de formato — isso é FluentValidation, não domínio
- Sem transição de estado relevante para o negócio

Se for o segundo caso, **pare e avise:** "Essa entidade parece CRUD puro. Manter anêmica é a decisão certa no padrão LMA."

### Etapa 2: Entrevista

1. Quais propriedades têm regras de mutação?
2. Para cada uma, qual o método de negócio? (verbo ubíquo)
3. Quais as pré-condições? (invariantes)
4. Há side effects? (eventos, integrações)
5. Quais Ações mutam a entidade diretamente? (precisam ser refatoradas)

### Etapa 3: Refatoração da entidade em `MinhaApp.Domain`

```csharp
// Antes — anêmica
public class Pedido
{
    public StatusPedido Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<ItemPedido> Itens { get; set; } = new();
}
```

```csharp
// Depois — rica em MinhaApp.Domain/Modulos/Checkout/Pedido.cs
namespace MinhaApp.Domain.Modulos.Checkout;

public class Pedido
{
    public Guid Id { get; private set; }
    public StatusPedido Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public decimal Total { get; private set; }

    private readonly List<ItemPedido> _itens = new();
    public IReadOnlyList<ItemPedido> Itens => _itens.AsReadOnly();

    private Pedido() { }   // EF Core reidrata por aqui

    public static Pedido Criar(Guid clienteId, IEnumerable<ItemPedido> itens)
    {
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            Status = StatusPedido.Aberto,
            CriadoEm = DateTime.UtcNow
        };
        pedido._itens.AddRange(itens);
        pedido.RecalcularTotal();
        return pedido;
    }

    public Result Cancelar(string motivo)
    {
        if (Status == StatusPedido.Entregue)
            return Result.Failure("Pedido entregue não pode ser cancelado");
        if (Status == StatusPedido.Cancelado)
            return Result.Failure("Pedido já está cancelado");
        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure("Motivo é obrigatório");

        Status = StatusPedido.Cancelado;
        return Result.Success();
    }

    private void RecalcularTotal() =>
        Total = _itens.Sum(i => i.Preco * i.Quantidade);
}
```

### Etapa 4: Refatoração das Ações

```csharp
// Antes — mutação direta na Ação
public class CancelarPedido(AppDbContext db)
{
    public async Task<Result> Execute(Guid id, string motivo, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return Result.Failure("Não encontrado");
        if (pedido.Status == StatusPedido.Entregue)   // ← regra na Ação
            return Result.Failure("Não pode cancelar");
        pedido.Status = StatusPedido.Cancelado;        // ← mutação direta
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

```csharp
// Depois — Ação só orquestra
public class CancelarPedido(AppDbContext db)
{
    public async Task<Result> Execute(Guid id, string motivo, CancellationToken ct)
    {
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null) return Result.Failure("Não encontrado");

        var resultado = pedido.Cancelar(motivo);       // ← chama Domain
        if (resultado.IsFailure) return resultado;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### Etapa 5: Configuração EF Core

```csharp
// Infrastructure/Database/Configurations/PedidoConfiguration.cs
public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.Total).HasPrecision(18, 2);
        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey("PedidoId");
        builder.Metadata
            .FindNavigation(nameof(Pedido.Itens))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

### Checklist de validação

- [ ] Setters privados em propriedades de comportamento
- [ ] Construtor privado, factory `Criar(...)` como ponto de entrada
- [ ] Métodos com vocabulário ubíquo (não `Set*`, `Update*`)
- [ ] Métodos retornam `Result` para falhas de regra
- [ ] Coleções como `IReadOnlyList<T>` com backing field privado
- [ ] EF Core configurado para reconhecer backing fields
- [ ] Ações refatoradas para chamar método de domínio
- [ ] `MinhaApp.Domain` **não** importa nada de infra (compilar para validar)
- [ ] Testes unitários do método de domínio criados/atualizados

## Quando NÃO refatorar

Mantenha anêmica se:
- Só validações de formato (FluentValidation cuida)
- CRUD puro sem transição de estado
- Não existe verbo de negócio natural para o método
