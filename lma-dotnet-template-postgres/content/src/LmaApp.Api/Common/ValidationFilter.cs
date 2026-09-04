using FluentValidation;

namespace LmaApp.Api.Common;

/// <summary>
/// Filtro global de validação LMA.
/// Aplique no grupo de endpoints do módulo via .AddEndpointFilter&lt;ValidationFilter&gt;().
/// A Ação assume que o Request chegou válido — não precisa revalidar.
/// </summary>
public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Localiza o argumento que é um Request (convenção: classe termina em "Request")
        var request = context.Arguments
            .FirstOrDefault(a => a is not null && a.GetType().Name.EndsWith("Request"));

        if (request is not null)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
            var validator = context.HttpContext.RequestServices
                .GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                var validationContext = new ValidationContext<object>(request);
                var validationResult = await validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());

                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
