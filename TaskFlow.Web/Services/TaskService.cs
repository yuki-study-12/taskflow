using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Contracts.Boards;

namespace TaskFlow.Web.Services;

public sealed class TaskService(IHttpClientFactory httpClientFactory, CustomAuthStateProvider authStateProvider)
{
    private const string HttpClientName = "TaskFlowApi";
    private static readonly string[] DefaultErrors = ["通信エラーが発生しました。時間をおいて再度お試しください。"];

    public async Task<ApiResult<IReadOnlyList<MyTaskResponse>>> GetMyTasksAsync()
    {
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks/mine");
            var token = await authStateProvider.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<IReadOnlyList<MyTaskResponse>>.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<IReadOnlyList<MyTaskResponse>>.Failure(DefaultErrors);
        }

        if (!response.IsSuccessStatusCode)
            return ApiResult<IReadOnlyList<MyTaskResponse>>.Failure(DefaultErrors);

        var tasks = await response.Content.ReadFromJsonAsync<IReadOnlyList<MyTaskResponse>>();
        return ApiResult<IReadOnlyList<MyTaskResponse>>.Success(tasks ?? []);
    }
}
