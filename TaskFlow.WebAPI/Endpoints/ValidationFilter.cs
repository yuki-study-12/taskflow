using System.ComponentModel.DataAnnotations;

namespace TaskFlow.WebAPI.Endpoints;

/// <summary>
/// DataAnnotations によるリクエストボディ検証フィルター。
/// エラーフォーマットを BadRequest&lt;IReadOnlyList&lt;string&gt;&gt; に統一する。
/// </summary>
internal sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().SingleOrDefault();
        if (argument is null)
            return Results.BadRequest(
                (IReadOnlyList<string>)["Request body is required."]);

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                argument,
                new ValidationContext(argument),
                results,
                validateAllProperties: true))
        {
            var errors = results
                .Select(r => r.ErrorMessage ?? "Validation error.")
                .ToList();
            return Results.BadRequest((IReadOnlyList<string>)errors);
        }

        return await next(context);
    }
}
