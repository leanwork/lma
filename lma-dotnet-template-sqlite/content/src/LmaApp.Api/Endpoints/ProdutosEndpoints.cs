using LmaApp.Api.Common;
using LmaApp.Api.Modulos.Produtos.BuscarProdutos;
using LmaApp.Api.Modulos.Produtos.CriarProduto;
using LmaApp.Api.Modulos.Produtos.DesativarProduto;

namespace LmaApp.Api.Endpoints;

/// <summary>
/// Roteamento centralizado do módulo Produtos.
/// Toda rota do módulo é mapeada aqui — a Ação não conhece HTTP.
/// Protocolo web (JWT, headers, route params) é resolvido aqui antes de chamar a Ação.
/// </summary>
public static class ProdutosEndpoints
{
    public static IEndpointRouteBuilder MapProdutosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/produtos")
            .WithTags("Produtos")
            .AddEndpointFilter<ValidationFilter>();
            // .RequireAuthorization(); // descomente para proteger o módulo

        // GET /produtos — buscar com filtros e paginação
        grupo.MapGet("/", async (
            [AsParameters] BuscarProdutosRequest request,
            BuscarProdutos acao,
            CancellationToken ct) =>
        {
            var resultado = await acao.Execute(request, ct);
            return resultado.IsSuccess
                ? Results.Ok(resultado.Value)
                : Results.BadRequest(resultado.Error);
        })
        .WithName("BuscarProdutos")
        .WithSummary("Busca produtos com filtros e paginação");

        // POST /produtos — criar produto
        grupo.MapPost("/", async (
            CriarProdutoRequest body,
            CriarProduto acao,
            CancellationToken ct) =>
        {
            var resultado = await acao.Execute(body, ct);
            return resultado.IsSuccess
                ? Results.Created($"/produtos/{resultado.Value!.Id}", resultado.Value)
                : Results.BadRequest(resultado.Error);
        })
        .WithName("CriarProduto")
        .WithSummary("Cria um novo produto");

        // DELETE /produtos/{id} — desativar produto (soft delete com regra)
        grupo.MapDelete("/{id:guid}", async (
            Guid id,
            DesativarProduto acao,
            CancellationToken ct) =>
        {
            // ID da rota → passado limpo para a Ação via Request
            var request = new DesativarProdutoRequest { ProdutoId = id };
            var resultado = await acao.Execute(request, ct);
            return resultado.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(resultado.Error);
        })
        .WithName("DesativarProduto")
        .WithSummary("Desativa um produto (soft delete)");

        return app;
    }
}
