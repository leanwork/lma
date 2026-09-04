using LmaApp.Domain.Modulos.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LmaApp.Api.Infrastructure.Database.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Descricao)
            .HasMaxLength(2000);

        builder.Property(p => p.Preco)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.EstoqueDisponivel)
            .IsRequired();

        builder.Property(p => p.Ativo)
            .IsRequired();

        builder.Property(p => p.CriadoEm)
            .IsRequired();

        // Índice para buscas por nome
        builder.HasIndex(p => p.Nome);

        // Índice para filtro por status ativo
        builder.HasIndex(p => p.Ativo);
    }
}
