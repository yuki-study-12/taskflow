using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TaskFlow.Web.Services;

public sealed class CustomAuthStateProvider(IJSRuntime jsRuntime) : AuthenticationStateProvider
{
    private const string TokenStorageKey = "authToken";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private string? _cachedToken;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await GetTokenAsync();
        var principal = string.IsNullOrWhiteSpace(token) ? Anonymous : BuildPrincipal(token);
        return new AuthenticationState(principal);
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken is not null)
            return _cachedToken;

        try
        {
            _cachedToken = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop isn't available yet (e.g. during prerendering); treat as unauthenticated for now.
            return null;
        }

        return _cachedToken;
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, token);
        _cachedToken = token;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(token))));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        _cachedToken = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal BuildPrincipal(string token)
    {
        var identity = new ClaimsIdentity(
            ParseClaimsFromJwt(token),
            authenticationType: "jwt",
            nameType: "name",
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Encoding.UTF8.GetString(ParseBase64WithoutPadding(payload));
        var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];

        foreach (var (type, value) in claims)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    yield return new Claim(type, item.ToString());
            }
            else
            {
                yield return new Claim(type, value.ToString());
            }
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        base64 = (base64.Length % 4) switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64,
        };

        return Convert.FromBase64String(base64);
    }
}
