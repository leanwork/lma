using LmaApp.Domain.Modulos.Produtos;

namespace LmaApp.Api.Modulos.Produtos.CriarProduto;

/// <summary>
/// Template D — Escrita com regra de negócio.
/// Regra vive em Produto.Criar() no Domain.
/// </summary>
public class CriarProduto(AppDbContext db, ILogger<CriarProduto> logger)
{
    public async Task<Result<CriarProdutoResponse>> Execute(
        CriarProdutoRequest request,
        CancellationToken ct)
    {
        try
        {
            var resultado = Produto.Criar(
                request.Nome,
                request.Descricao,
                request.Preco,
                request.EstoqueInicial);

            if (resultado.IsFailure)
            {
                logger.LogWarning(
                    "CriarProduto: falha de negócio. Nome={Nome} Erro={Erro}",
                    request.Nome, resultado.Error);
                return Result.Failure<CriarProdutoResponse>(resultado.Error!);
            }

            var produto = resultado.Value!;
            db.Produtos.Add(produto);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "CriarProduto: produto criado. Id={ProdutoId} Nome={Nome}",
                produto.Id, produto.Nome);

            return Result.Success(new CriarProdutoResponse(
                produto.Id, produto.Nome, produto.Preco, produto.EstoqueDisponivel));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao criar produto. Nome={Nome}", request.Nome);
            return Result.Failure<CriarProdutoResponse>(
                "Não foi possível criar o produto. Tente novamente.");
        }
    }
}
