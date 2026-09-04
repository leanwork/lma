using FluentAssertions;
using LmaApp.Domain.Modulos.Produtos;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LmaApp.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Testes de UNIDADE — Domain puro, sem mocks, sem banco
// ─────────────────────────────────────────────────────────────────────────────

public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarProdutoAtivo()
    {
        var resultado = Produto.Criar("Notebook", "Notebook gamer", 5999.99m, 10);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value!.Nome.Should().Be("Notebook");
        resultado.Value.Preco.Should().Be(5999.99m);
        resultado.Value.EstoqueDisponivel.Should().Be(10);
        resultado.Value.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Criar_SemNome_DeveFalhar()
    {
        var resultado = Produto.Criar("", "Descrição", 99m, 5);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Nome");
    }

    [Fact]
    public void Criar_ComPrecoZero_DeveFalhar()
    {
        var resultado = Produto.Criar("Produto", "Desc", 0m, 5);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Preço");
    }

    [Fact]
    public void Desativar_ProdutoAtivo_DeveDesativar()
    {
        var produto = Produto.Criar("X", "Desc", 10m, 5).Value!;

        var resultado = produto.Desativar();

        resultado.IsSuccess.Should().BeTrue();
        produto.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_ProdutoJaInativo_DeveFalhar()
    {
        var produto = Produto.Criar("X", "Desc", 10m, 5).Value!;
        produto.Desativar();

        var resultado = produto.Desativar();

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("inativo");
    }

    [Fact]
    public void ReservarEstoque_ComQuantidadeSuficiente_DeveReduzirEstoque()
    {
        var produto = Produto.Criar("X", "Desc", 10m, 10).Value!;

        var resultado = produto.ReservarEstoque(3);

        resultado.IsSuccess.Should().BeTrue();
        produto.EstoqueDisponivel.Should().Be(7);
    }

    [Fact]
    public void ReservarEstoque_SemEstoqueSuficiente_DeveFalhar()
    {
        var produto = Produto.Criar("X", "Desc", 10m, 2).Value!;

        var resultado = produto.ReservarEstoque(5);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Estoque insuficiente");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Testes de INTEGRAÇÃO — Ações via HTTP com banco real
// ─────────────────────────────────────────────────────────────────────────────

public class ProdutosIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProdutosIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BuscarProdutos_SemFiltros_DeveRetornar200()
    {
        var response = await _client.GetAsync("/produtos?Pagina=1&Tamanho=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriarProduto_ComDadosValidos_DeveRetornar201()
    {
        var payload = new
        {
            Nome = "Produto Teste",
            Descricao = "Descrição do produto",
            Preco = 99.90m,
            EstoqueInicial = 50
        };

        var response = await _client.PostAsJsonAsync("/produtos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CriarProduto_SemNome_DeveRetornar400()
    {
        var payload = new { Nome = "", Descricao = "Desc", Preco = 10m, EstoqueInicial = 5 };

        var response = await _client.PostAsJsonAsync("/produtos", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DesativarProduto_ProdutoInexistente_DeveRetornar400()
    {
        var id = Guid.NewGuid(); // ID que não existe

        var response = await _client.DeleteAsync($"/produtos/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
