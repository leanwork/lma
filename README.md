# LMA — Lean Modular Architecture

**Templates `dotnet new` para APIs .NET seguindo a Lean Modular Architecture v1.0.**

Cada template gera uma solução pronta para rodar: dois projetos físicos, Minimal API, EF Core,
observabilidade configurada, testes de integração passando e a pasta `.claude/` com skills,
commands, subagent e hooks que ensinam a IA a escrever código no padrão.

```bash
git clone https://github.com/leanwork/lma.git
dotnet new install ./lma/lma-dotnet-template-sqlite

dotnet new lma-sqlite -n MinhaApi
cd MinhaApi && dotnet run --project src/MinhaApi.Api
```

O template SQLite sobe sem nenhuma infraestrutura externa — bom para ver a arquitetura
funcionando em 30 segundos. Para SQL Server ou PostgreSQL, veja [Os templates](#os-templates).

---

## O que é a LMA

Arquitetura **API-first** para .NET, desenhada para ir direto ao ponto sem cerimônia teórica.
Serve tanto para um MVP de uma pessoa em duas semanas quanto para um sistema com time de 15+ devs
— a estrutura é a mesma, o que muda é a quantidade de módulos.

> **Princípio fundador:** separe por módulo de negócio e por ação. Isole o que muda de verdade —
> e o que muda é a infraestrutura externa, não o banco. Otimize para que a estrutura seja
> previsível e gerável.

Três ideias sustentam isso:

**1. Modularidade por negócio.** O código se organiza por bounded context (Catálogo, Checkout,
Faturamento), não por camada técnica.

**2. Isolamento seletivo.** Clean Architecture isola tudo "por via das dúvidas". A LMA isola só o
que realmente troca — gateways de pagamento, storage, mensageria. **Não cria abstração de
persistência**, porque o banco raramente troca de verdade.

**3. Otimização para produtividade humana E de IA.** Boa parte do código hoje é escrita com
LLMs. A LMA assume isso como premissa de design: estrutura previsível, vocabulário fixo, decisões
já tomadas. Menos ambiguidade significa menos variação inventada pela IA — e os dois projetos
físicos viram um guardrail que o **compilador** valida.

### Como o código fica

Uma **Ação** é um vertical slice: uma classe, um método `Execute`, retorno `Result<T>`.
Sem `Handler`, sem `Service`, sem MediatR, sem Repository.

```csharp
public class CriarProduto(AppDbContext db, ILogger<CriarProduto> logger)
{
    public async Task<Result<CriarProdutoResponse>> Execute(
        CriarProdutoRequest request, CancellationToken ct)
    {
        var resultado = Produto.Criar(
            request.Nome, request.Descricao, request.Preco, request.EstoqueInicial);
        if (resultado.IsFailure)
            return Result.Failure<CriarProdutoResponse>(resultado.Error!);

        db.Produtos.Add(resultado.Value!);
        await db.SaveChangesAsync(ct);   // commit sempre explícito

        return Result.Success(new CriarProdutoResponse(/* ... */));
    }
}
```

A Ação nunca conhece HTTP. O roteamento é centralizado por módulo:

```csharp
// Endpoints/ProdutosEndpoints.cs — TODAS as rotas do módulo
var grupo = app.MapGroup("/produtos")
    .WithTags("Produtos")
    .AddEndpointFilter<ValidationFilter>();   // validação automática

grupo.MapPost("/", async (CriarProdutoRequest body, CriarProduto acao, CancellationToken ct) =>
{
    var r = await acao.Execute(body, ct);
    return r.IsSuccess ? Results.Created($"/produtos/{r.Value!.Id}", r.Value)
                       : Results.BadRequest(r.Error);
});
```

### As regras invioláveis

Dois projetos físicos, e o compilador é o guardião:

```
MinhaApp.Api  ──referencia──▶  MinhaApp.Domain
                               (zero infra: sem EF, sem ASP.NET, sem FluentValidation)
```

Os 7 "nunca faça":

1. Ação nunca injeta outra Ação
2. Domain nunca importa framework de infra
3. Ação nunca vê `HttpContext` ou `ClaimsPrincipal`
4. Nunca criar Repository ou Writer
5. Ação nunca declara a própria rota
6. Ação nunca valida formato manualmente
7. Ação de escrita nunca retorna sem `SaveChangesAsync(ct)`

Documento completo (20 seções) em
[`docs/architecture/lma-v1.0.md`](lma-dotnet-template-sqlserver/content/docs/architecture/lma-v1.0.md),
que também vai junto em todo projeto gerado.

---

## Os templates

| Template | Short name | Banco |
|---|---|---|
| [`lma-dotnet-template-sqlserver`](lma-dotnet-template-sqlserver) | `lma-mssql` | SQL Server / Azure SQL |
| [`lma-dotnet-template-postgres`](lma-dotnet-template-postgres) | `lma-pg` | PostgreSQL (Npgsql) |
| [`lma-dotnet-template-sqlite`](lma-dotnet-template-sqlite) | `lma-sqlite` | SQLite |

Os três são idênticos exceto pelo provider EF Core, connection string, migration inicial e
`docker-compose.yml`.

### Instalação

```bash
git clone https://github.com/leanwork/lma.git
cd lma

dotnet new install ./lma-dotnet-template-sqlserver
dotnet new install ./lma-dotnet-template-postgres
dotnet new install ./lma-dotnet-template-sqlite
```

### Uso

```bash
dotnet new lma-pg -n Faturamento
cd Faturamento

docker compose up -d                        # banco + Jaeger + Seq
dotnet run --project src/Faturamento.Api    # migration aplicada automaticamente
dotnet test                                 # 11 testes de integração, sem Docker
```

### Opções

| Opção | Valores | Padrão | O que faz |
|---|---|---|---|
| `--Framework` | `net10.0`, `net9.0`, `net8.0` | `net10.0` | TFM e alinhamento das versões de pacote |
| `--IncludeExampleModule` | `true`, `false` | `true` | Módulo Produtos completo (3 Ações + testes) |
| `--IncludeTests` | `true`, `false` | `true` | Projeto de testes de integração |
| `--UseSeq` | `true`, `false` | `false` | Sink Serilog para Seq |

```bash
# Projeto limpo, sem exemplo, em .NET 8 LTS
dotnet new lma-mssql -n MinhaApi --IncludeExampleModule false --Framework net8.0
```

---

## O que vem no projeto gerado

```
src/
├── MinhaApi.Domain/            # Entidades ricas, Value Objects, Result<T> — zero infra
└── MinhaApi.Api/
    ├── Modulos/{Modulo}/{Acao}/    # Vertical slices
    ├── Endpoints/                  # Roteamento centralizado por módulo
    ├── Infrastructure/Database/    # AppDbContext + configurations
    └── Common/                     # ValidationFilter, ExceptionMiddleware, Serilog, OTel

tests/MinhaApi.Tests/           # Integração via WebApplicationFactory + EF InMemory
.claude/                        # Skills, commands, subagent e hooks LMA
docs/architecture/              # LMA v1.0, templates de Ação, checklist de PR
docker-compose.yml              # Banco + Jaeger + Seq
CLAUDE.md                       # Guia de arquitetura para IA
```

**Stack:** Minimal API, EF Core, Dapper, FluentValidation (via `IEndpointFilter`),
Serilog, OpenTelemetry (OTLP → Jaeger), Swashbuckle, xUnit + FluentAssertions.

### Suporte a IA embutido

Todo projeto gerado já vem com `.claude/` configurada — o Claude Code carrega tudo
automaticamente, sem instalação:

- **3 skills** que ativam por contexto — `lma-create-action`, `lma-add-module`,
  `lma-refactor-to-rich`. Basta dizer *"criar ação para listar clientes"*.
- **3 commands** — `/lma-docs`, `/lma-review`, `/lma-init`.
- **1 subagent** — `lma-reviewer`, para revisão arquitetural profunda.
- **2 hooks** — validam as 8 convenções LMA a cada `Write`/`Edit` em `.cs` e devolvem as
  violações para o Claude corrigir sozinho. Requerem bash (Git Bash ou WSL no Windows).

O `CLAUDE.md` na raiz também serve Cursor, Copilot e afins.

---

## Estado dos templates

Validados com **.NET SDK 10.0.400**: os 3 templates × 6 combinações de opções compilam com
0 erros e 0 avisos, e os 11 testes de integração passam em `net8.0`, `net9.0` e `net10.0`.

Notas:

- Os testes usam EF Core InMemory e **não exigem Docker**. Os templates SQL Server e PostgreSQL
  já referenciam o Testcontainers correspondente — basta descomentar o container em
  `tests/*/ApiFactory.cs` para rodar contra um banco real.
- `FluentAssertions` está fixado em `7.*`, a última versão sob Apache-2.0. A 8.x exige licença
  comercial Xceed.
- As versões dos pacotes Microsoft/EF Core seguem o `--Framework` escolhido via a propriedade
  `MsPackageVersion` no `.csproj`. Os demais pacotes usam versão flutuante (`*`).

## Licença

MIT — Leanwork Group. Ver [LICENSE](LICENSE).
