using LmaApp.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LmaApp.Tests;

/// <summary>
/// Factory para testes de integração.
/// Usa banco InMemory por padrão — sem Docker necessário.
/// Para testes com banco real, substitua por Testcontainers (ver comentário abaixo).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // ── Testcontainers (opcional) ─────────────────────────────────────────────
    // Para testes com banco real, descomente o container desejado e ajuste
    // o ConfigureWebHost abaixo:
    //
    // private readonly MsSqlContainer _db = new MsSqlBuilder()
    //     .WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
    //
    // private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
    //     .WithImage("postgres:16-alpine").Build();

    public Task InitializeAsync() => Task.CompletedTask;
    // Com Testcontainers: public async Task InitializeAsync() => await _db.StartAsync();

    public new Task DisposeAsync() => Task.CompletedTask;
    // Com Testcontainers: public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Sem isto o host sobe como Development e o Program.cs chama MigrateAsync(),
        // que nao existe no provider InMemory.
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove TODO o registro do EF (inclusive o provider real). Tirar apenas
            // DbContextOptions<T> deixa o provider para tras e o EF aborta com
            // "Only a single database provider can be registered".
            foreach (var d in services.Where(d =>
                         d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true
                         || d.ServiceType == typeof(AppDbContext)).ToList())
            {
                services.Remove(d);
            }

            // InMemory — rápido, sem dependência de Docker
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("LmaApp_Tests"));

            // Com Testcontainers SQL Server:
            // services.AddDbContext<AppDbContext>(opt =>
            //     opt.UseSqlServer(_db.GetConnectionString()));

            // Com Testcontainers PostgreSQL:
            // services.AddDbContext<AppDbContext>(opt =>
            //     opt.UseNpgsql(_db.GetConnectionString()));
        });
    }
}
