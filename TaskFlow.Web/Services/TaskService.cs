using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Contracts.Boards;

namespace TaskFlow.Web.Services;

public sealed class TaskService(IHttpClientFactory httpClientFactory, CustomAuthStateProvider authStateProvider)
{
    private const string HttpClientName = "TaskFlowApi";
    private static readonly string[] DefaultErrors = ["通信エラーが発生しました。時間をおいて再度お試しください。"];

    public Task<ApiResult<IReadOnlyList<MyTaskResponse>>> GetMyTasksAsync() =>
        SendAsync<IReadOnlyList<MyTaskResponse>>(HttpMethod.Get, "/api/tasks/mine");

    public Task<ApiResult<TaskResponse>> UpdateTaskAsync(
        Guid taskId, string title, string description, Guid? assigneeId, DateTime? dueDate, string priority) =>
        SendAsync<TaskResponse>(
            HttpMethod.Put,
            $"/api/tasks/{taskId}",
            new UpdateTaskRequest(title, description, assigneeId, dueDate, priority));

    public Task<ApiResult<IReadOnlyList<CommentResponse>>> GetCommentsAsync(Guid taskId) =>
        SendAsync<IReadOnlyList<CommentResponse>>(HttpMethod.Get, $"/api/tasks/{taskId}/comments");

    public Task<ApiResult<CommentResponse>> AddCommentAsync(Guid taskId, string body) =>
        SendAsync<CommentResponse>(HttpMethod.Post, $"/api/tasks/{taskId}/comments", new AddCommentRequest(body));

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
