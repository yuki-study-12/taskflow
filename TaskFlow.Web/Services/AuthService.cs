using System.Net.Http.Json;
using TaskFlow.Contracts.Auth;

namespace TaskFlow.Web.Services;

public sealed record AuthOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static AuthOperationResult Success() => new(true, []);
    public static AuthOperationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}

public sealed class AuthService(IHttpClientFactory httpClientFactory, CustomAuthStateProvider authStateProvider)
{
    private const string HttpClientName = "TaskFlowApi";
    private static readonly string[] DefaultErrors = ["通信エラーが発生しました。時間をおいて再度お試しください。"];

    public Task<AuthOperationResult> LoginAsync(string email, string password) =>
        SubmitAsync("/api/auth/login", new LoginRequest(email, password));

    public Task<AuthOperationResult> RegisterAsync(string email, string password, string displayName) =>
        SubmitAsync("/api/auth/register", new RegisterRequest(email, password, displayName));

    public Task LogoutAsync() => authStateProvider.MarkUserAsLoggedOutAsync();

    private async Task<AuthOperationResult> SubmitAsync<TRequest>(string uri, TRequest request)
    {
        HttpResponseMessage response;
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.PostAsJsonAsync(uri, request);
        }
        catch (HttpRequestException)
        {
            return AuthOperationResult.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return AuthOperationResult.Failure(DefaultErrors);
        }

        if (response.IsSuccessStatusCode)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            await authStateProvider.MarkUserAsAuthenticatedAsync(auth!.Token);
            return AuthOperationResult.Success();
        }

        var errors = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        return AuthOperationResult.Failure(errors is { Count: > 0 } ? errors : DefaultErrors);
    }
}
