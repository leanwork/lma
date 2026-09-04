namespace LmaApp.Api.Modulos.Produtos.CriarProduto;

public record CriarProdutoRequest(
    string Nome,
    string Descricao,
    decimal Preco,
    int EstoqueInicial);

public record CriarProdutoResponse(
    Guid Id,
    string Nome,
    decimal Preco,
    int Estoque);

public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Preco).GreaterThan(0);
        RuleFor(x => x.EstoqueInicial).GreaterThanOrEqualTo(0);
    }
}
