namespace LmaApp.Api.Modulos.Produtos.BuscarProdutos;

public record BuscarProdutosRequest(
    string? Termo = null,
    decimal? PrecoMinimo = null,
    decimal? PrecoMaximo = null,
    bool IncluirInativos = false,
    int Pagina = 1,
    int Tamanho = 20);

public record BuscarProdutosResponse(
    List<ProdutoItem> Itens,
    int Total,
    int Pagina,
    int Tamanho);

public record ProdutoItem(
    Guid Id,
    string Nome,
    string Descricao,
    decimal Preco,
    int EstoqueDisponivel,
    bool Ativo);

public class BuscarProdutosValidator : AbstractValidator<BuscarProdutosRequest>
{
    public BuscarProdutosValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.Tamanho).InclusiveBetween(1, 100);
        RuleFor(x => x.Termo).MaximumLength(200).When(x => x.Termo is not null);
        RuleFor(x => x.PrecoMinimo).GreaterThanOrEqualTo(0).When(x => x.PrecoMinimo.HasValue);
        RuleFor(x => x.PrecoMaximo)
            .GreaterThan(x => x.PrecoMinimo ?? 0)
            .When(x => x.PrecoMaximo.HasValue && x.PrecoMinimo.HasValue)
            .WithMessage("Preço máximo deve ser maior que o mínimo");
    }
}
