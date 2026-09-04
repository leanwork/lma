using LmaApp.Domain.Modulos._Common;

namespace LmaApp.Domain.Modulos.Produtos;

/// <summary>
/// Entidade rica de exemplo.
/// Setters privados — estado só muda via métodos de negócio.
/// Construtor privado — instancie via Criar().
/// </summary>
public class Produto
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public int EstoqueDisponivel { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // Construtor privado: EF Core reidrata por aqui via reflection
    private Produto() { }

    /// <summary>Factory method — único ponto de criação de Produto.</summary>
    public static Result<Produto> Criar(string nome, string descricao, decimal preco, int estoque)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Produto>("Nome é obrigatório");

        if (preco <= 0)
            return Result.Failure<Produto>("Preço deve ser maior que zero");

        if (estoque < 0)
            return Result.Failure<Produto>("Estoque não pode ser negativo");

        return Result.Success(new Produto
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            Descricao = descricao.Trim(),
            Preco = preco,
            EstoqueDisponivel = estoque,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        });
    }

    /// <summary>Atualiza dados do produto respeitando invariantes.</summary>
    public Result Atualizar(string nome, string descricao, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("Nome é obrigatório");

        if (preco <= 0)
            return Result.Failure("Preço deve ser maior que zero");

        Nome = nome.Trim();
        Descricao = descricao.Trim();
        Preco = preco;

        return Result.Success();
    }

    /// <summary>Desativa o produto. Produto inativo não aparece no catálogo.</summary>
    public Result Desativar()
    {
        if (!Ativo)
            return Result.Failure("Produto já está inativo");

        Ativo = false;
        return Result.Success();
    }

    /// <summary>Reativa produto previamente desativado.</summary>
    public Result Ativar()
    {
        if (Ativo)
            return Result.Failure("Produto já está ativo");

        Ativo = true;
        return Result.Success();
    }

    /// <summary>Reserva unidades do estoque. Retorna falha se não há quantidade suficiente.</summary>
    public Result ReservarEstoque(int quantidade)
    {
        if (quantidade <= 0)
            return Result.Failure("Quantidade deve ser maior que zero");

        if (!Ativo)
            return Result.Failure("Produto inativo não pode ter estoque reservado");

        if (EstoqueDisponivel < quantidade)
            return Result.Failure($"Estoque insuficiente. Disponível: {EstoqueDisponivel}");

        EstoqueDisponivel -= quantidade;
        return Result.Success();
    }
}
