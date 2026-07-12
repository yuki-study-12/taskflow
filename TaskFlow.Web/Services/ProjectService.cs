using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Contracts.Projects;

namespace TaskFlow.Web.Services;

public sealed class ProjectService(IHttpClientFactory httpClientFactory, CustomAuthStateProvider authStateProvider)
{
    private const string HttpClientName = "TaskFlowApi";
    private static readonly string[] DefaultErrors = ["通信エラーが発生しました。時間をおいて再度お試しください。"];

    public async Task<ApiResult<IReadOnlyList<ProjectResponse>>> GetProjectsAsync()
    {
        HttpResponseMessage response;
        try
        {
            var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "/api/projects");
            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<IReadOnlyList<ProjectResponse>>.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<IReadOnlyList<ProjectResponse>>.Failure(DefaultErrors);
        }

        if (!response.IsSuccessStatusCode)
            return ApiResult<IReadOnlyList<ProjectResponse>>.Failure(DefaultErrors);

        var projects = await response.Content.ReadFromJsonAsync<IReadOnlyList<ProjectResponse>>();
        return ApiResult<IReadOnlyList<ProjectResponse>>.Success(projects ?? []);
    }

    public async Task<ApiResult<ProjectResponse>> CreateProjectAsync(string name, string description)
    {
        HttpResponseMessage response;
        try
        {
            var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, "/api/projects");
            request.Content = JsonContent.Create(new CreateProjectRequest(name, description));
            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<ProjectResponse>.Failure(DefaultErrors);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<ProjectResponse>.Failure(DefaultErrors);
        }

        if (response.IsSuccessStatusCode)
        {
            var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
            return ApiResult<ProjectResponse>.Success(project!);
        }

        var errors = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>();
        return ApiResult<ProjectResponse>.Failure(errors is { Count: > 0 } ? errors : DefaultErrors);
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        var token = await authStateProvider.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return request;
    }
}
