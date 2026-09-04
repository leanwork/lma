using System.Net;
using System.Text.Json;

namespace LmaApp.Api.Common;

/// <summary>
/// Middleware global de tratamento de exceções.
/// Captura exceções não tratadas, loga com contexto completo via Serilog
/// e retorna ProblemDetails padronizado — nunca vaza stack trace para o cliente.
/// </summary>
public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Log estruturado com contexto completo da requisição
        logger.LogError(ex,
            "Exceção não tratada. Method={Method} Path={Path} QueryString={QueryString} " +
            "TraceId={TraceId} UserId={UserId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.TraceIdentifier,
            context.User?.FindFirst("sub")?.Value ?? "anonymous");

        var (statusCode, title) = ex switch
        {
            OperationCanceledException => (499, "Requisição cancelada pelo cliente"),
            TimeoutException           => (504, "Tempo de resposta esgotado"),
            UnauthorizedAccessException => (403, "Acesso não autorizado"),
            _                          => (500, "Erro interno do servidor")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            traceId = context.TraceIdentifier,
            // Detalhe só em Development — nunca em produção
            detail = context.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .IsDevelopment()
                    ? ex.Message
                    : null
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions));
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
