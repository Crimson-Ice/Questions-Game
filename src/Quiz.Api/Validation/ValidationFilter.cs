using FluentValidation;

namespace Quiz.Api.Validation;

/// <summary>
/// Filtre d'endpoint : valide le corps de requête de type <typeparamref name="TRequest"/> avec
/// FluentValidation, et répond 400 (ProblemDetails de validation) avant d'appeler le handler.
/// </summary>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
