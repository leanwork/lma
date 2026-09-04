using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LmaApp.Api.Common;

public static class OpenTelemetrySetup
{
    public static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName    = configuration["OpenTelemetry:ServiceName"] ?? "LmaApp";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var otlpEndpoint   = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] =
                    configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
            });

        services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation(opt =>
                {
                    opt.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint))
                .AddConsoleExporter()
            )
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint))
            );

        return services;
    }
}
