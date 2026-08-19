using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Contracts.Notifications;

namespace TaskFlow.Web.Services;

public sealed class NotificationService(IHttpClientFactory httpClientFactory, CustomAuthStateProvider authStateProvider)
{
    private const string HttpClientName = "TaskFlowApi";
    private static readonly string[] DefaultErrors = ["通信エラーが発生しました。時間をおいて再度お試しください。"];

    public Task<ApiResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync() =>
        SendAsync<IReadOnlyList<NotificationResponse>>(HttpMethod.Get, "/api/notifications");

    public Task<ApiResult<NotificationResponse>> MarkAsReadAsync(Guid notificationId) =>
        SendAsync<NotificationResponse>(HttpMethod.Put, $"/api/notifications/{notificationId}/read");

    public async Task<ApiResult<bool>> MarkAllAsReadAsync()
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/api/notifications/read-all");
            var token = await authStateProvider.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<bool>.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<bool>.Failure(DefaultErrors);
        }

        return response.IsSuccessStatusCode
            ? ApiResult<bool>.Success(true)
            : ApiResult<bool>.Failure(DefaultErrors);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string uri, object? body = null)
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(method, uri);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            var token = await authStateProvider.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Failure(DefaultErrors);
        }

        if (!response.IsSuccessStatusCode)
            return ApiResult<T>.Failure(DefaultErrors);

        var data = await response.Content.ReadFromJsonAsync<T>();
        return ApiResult<T>.Success(data!);
    }
}
