namespace LmaApp.Api.Modulos.Produtos.DesativarProduto;

/// <summary>
/// Template D — Escrita com regra de negócio.
/// Regra vive em Produto.Desativar() no Domain.
/// </summary>
public class DesativarProduto(AppDbContext db, ILogger<DesativarProduto> logger)
{
    public async Task<Result> Execute(
        DesativarProdutoRequest request,
        CancellationToken ct)
    {
        try
        {
            var produto = await db.Produtos
                .FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);

            if (produto is null)
            {
                logger.LogWarning(
                    "DesativarProduto: produto não encontrado. ProdutoId={ProdutoId}",
                    request.ProdutoId);
                return Result.Failure("Produto não encontrado");
            }

            var resultado = produto.Desativar();

            if (resultado.IsFailure)
            {
                logger.LogWarning(
                    "DesativarProduto: falha de negócio. ProdutoId={ProdutoId} Erro={Erro}",
                    request.ProdutoId, resultado.Error);
                return resultado;
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "DesativarProduto: produto desativado. ProdutoId={ProdutoId}",
                request.ProdutoId);

            return Result.Success();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Erro ao desativar produto. ProdutoId={ProdutoId}", request.ProdutoId);
            return Result.Failure("Não foi possível desativar o produto. Tente novamente.");
        }
    }
}
