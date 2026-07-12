namespace TaskFlow.Web.Services;

public sealed record ApiResult<T>(bool Succeeded, T? Data, IReadOnlyList<string> Errors)
{
    public static ApiResult<T> Success(T data) => new(true, data, []);
    public static ApiResult<T> Failure(IReadOnlyList<string> errors) => new(false, default, errors);
}
