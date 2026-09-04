namespace LmaApp.Api.Modulos.Produtos.BuscarProdutos;

/// <summary>
/// Template A — Leitura com EF Core. Sem Repository.
/// </summary>
public class BuscarProdutos(AppDbContext db, ILogger<BuscarProdutos> logger)
{
    public async Task<Result<BuscarProdutosResponse>> Execute(
        BuscarProdutosRequest request,
        CancellationToken ct)
    {
        try
        {
            var query = db.Produtos.AsNoTracking()
                .Where(p => p.Ativo || request.IncluirInativos);

            if (!string.IsNullOrWhiteSpace(request.Termo))
                query = query.Where(p =>
                    p.Nome.Contains(request.Termo) ||
                    p.Descricao.Contains(request.Termo));

            if (request.PrecoMinimo.HasValue)
                query = query.Where(p => p.Preco >= request.PrecoMinimo.Value);

            if (request.PrecoMaximo.HasValue)
                query = query.Where(p => p.Preco <= request.PrecoMaximo.Value);

            var total = await query.CountAsync(ct);

            var itens = await query
                .OrderBy(p => p.Nome)
                .Skip((request.Pagina - 1) * request.Tamanho)
                .Take(request.Tamanho)
                .Select(p => new ProdutoItem(
                    p.Id, p.Nome, p.Descricao,
                    p.Preco, p.EstoqueDisponivel, p.Ativo))
                .ToListAsync(ct);

            logger.LogDebug(
                "BuscarProdutos: {Total} produto(s). Termo={Termo} Pagina={Pagina}",
                total, request.Termo, request.Pagina);

            return Result.Success(
                new BuscarProdutosResponse(itens, total, request.Pagina, request.Tamanho));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao buscar produtos. Termo={Termo}", request.Termo);
            return Result.Failure<BuscarProdutosResponse>(
                "Não foi possível buscar produtos. Tente novamente.");
        }
    }
}
