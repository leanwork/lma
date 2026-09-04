namespace LmaApp.Api.Modulos.Produtos.DesativarProduto;

public record DesativarProdutoRequest
{
    // ProdutoId vem da rota — preenchido pelo Endpoint via 'with'
    public Guid ProdutoId { get; init; }
}
