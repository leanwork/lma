//#if (IncludeExampleModule)
using LmaApp.Domain.Modulos.Produtos;
//#endif
using Microsoft.EntityFrameworkCore;

namespace LmaApp.Api.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ─── DbSets por módulo ───────────────────────────────────────────────
//#if (IncludeExampleModule)
    public DbSet<Produto> Produtos => Set<Produto>();

//#endif
    // Adicione novos DbSets aqui quando criar novos módulos:
    // public DbSet<Pedido> Pedidos => Set<Pedido>();
    // public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as IEntityTypeConfiguration do assembly automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
