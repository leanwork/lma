using LmaApp.Api.Common;
//#if (IncludeExampleModule)
using LmaApp.Api.Endpoints;
using LmaApp.Api.Modulos.Produtos.BuscarProdutos;
using LmaApp.Api.Modulos.Produtos.CriarProduto;
using LmaApp.Api.Modulos.Produtos.DesativarProduto;
//#endif
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new Serilog.LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando LmaApp...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.AddSerilog();

    // ─── Banco de dados: SQL Server ───────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(3)));

    // ─── Validação ────────────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // ─── OpenTelemetry ────────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry(builder.Configuration);

    // ─── Ações ────────────────────────────────────────────────────────────────
//#if (IncludeExampleModule)
    builder.Services.AddScoped<BuscarProdutos>();
    builder.Services.AddScoped<CriarProduto>();
    builder.Services.AddScoped<DesativarProduto>();
//#endif

    // ─── Gateways de infraestrutura externa ───────────────────────────────────
    // Exemplo: builder.Services.AddScoped<IPagamentoGateway, PagarMeGateway>();

    // ─── API / Docs ───────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
        c.SwaggerDoc("v1", new()
        {
            Title = "LmaApp API",
            Version = "v1",
            Description = "Lean Modular Architecture (LMA) v1.0 — Leanwork Group"
        }));

    var app = builder.Build();

    // ─── Middleware pipeline ──────────────────────────────────────────────────
    app.UseExceptionHandling();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000}ms";
        opts.GetLevel = (ctx, _, _) =>
            ctx.Request.Path.StartsWithSegments("/health")
                ? Serilog.Events.LogEventLevel.Verbose
                : Serilog.Events.LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // ─── Endpoints ────────────────────────────────────────────────────────────
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "ok",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    }))
    .WithTags("Health")
    .WithName("Health")
    .AllowAnonymous();

//#if (IncludeExampleModule)
    app.MapProdutosEndpoints();
//#endif
    // app.MapClientesEndpoints();
    // app.MapCheckoutEndpoints();

    Log.Information("LmaApp iniciado com sucesso");
    await app.RunAsync();
}
catch (Exception ex) when (ex.GetType().Name is not "StopTheHostException" and not "HostAbortedException")
{
    Log.Fatal(ex, "LmaApp falhou na inicialização");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
