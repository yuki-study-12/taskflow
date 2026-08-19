using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Models;
using TaskFlow.Infrastructure.ErrorAnalysis;
using Xunit;

namespace TaskFlow.Infrastructure.Tests.ErrorAnalysis;

public class ClaudeExceptionAnalyzerTests
{
    // ─── テスト用ヘルパー ────────────────────────────────────────────────

    /// <summary>StackTrace をオーバーライドできる例外クラス。</summary>
    private sealed class FakeException(string message, string fakeStackTrace)
        : Exception(message)
    {
        public override string? StackTrace { get; } = fakeStackTrace;
    }

    private sealed class FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        string         responseJson) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken _)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static ClaudeExceptionAnalyzer CreateAnalyzer(
        FakeHttpMessageHandler handler,
        ErrorAnalysisSettings? settings = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var factory    = new FakeHttpClientFactory(httpClient);
        var opts       = Options.Create(settings ?? new ErrorAnalysisSettings { Enabled = true });
        return new ClaudeExceptionAnalyzer(factory, opts);
    }

    private static ExceptionContext BuildContext(Exception? ex = null) =>
        new(
            ex ?? new InvalidOperationException("test error"),
            "POST /api/tasks",
            "user-123",
            "Production",
            DateTimeOffset.UtcNow);

    private static string ValidClaudeResponse(
        string   rootCause      = "テスト原因",
        string[] ? files        = null,
        string   impact         = "影響なし",
        string   recommendedFix = "修正方針")
    {
        var filesJson = System.Text.Json.JsonSerializer.Serialize(
            files ?? ["Foo/Bar.cs (line 1)"]);
        return $$"""
            {
              "content": [{
                "type": "text",
                "text": "{\"rootCause\":\"{{rootCause}}\",\"affectedFiles\":{{filesJson}},\"impact\":\"{{impact}}\",\"recommendedFix\":\"{{recommendedFix}}\"}"
              }]
            }
            """;
    }

    // ─── テスト ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_Enabledがfalseの場合_APIを呼ばずにEmptyを返す()
    {
        var handler  = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var settings = new ErrorAnalysisSettings { Enabled = false };
        var analyzer = CreateAnalyzer(handler, settings);

        var result = await analyzer.AnalyzeAsync(BuildContext());

        Assert.Equal(ErrorAnalysis.Empty, result);
        Assert.Null(handler.LastRequest); // HTTP 呼び出しがないこと
    }

    [Fact]
    public async Task AnalyzeAsync_正常レスポンス_ErrorAnalysisにマップされる()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ValidClaudeResponse(
            rootCause      : "Null参照",
            files          : ["TaskFlow.Application/Boards/Commands/CreateTaskCommand.cs (line 42)"],
            impact         : "タスク作成が全件失敗",
            recommendedFix : "nullチェックを追加"));
        var analyzer = CreateAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(BuildContext());

        Assert.Equal("Null参照", result.RootCause);
        Assert.Single(result.AffectedFiles);
        Assert.Equal("タスク作成が全件失敗", result.Impact);
        Assert.Equal("nullチェックを追加", result.RecommendedFix);
    }

    [Fact]
    public async Task AnalyzeAsync_APIが失敗した場合_例外をスローする()
    {
        var handler  = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        var analyzer = CreateAnalyzer(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => analyzer.AnalyzeAsync(BuildContext()));
    }

    [Fact]
    public async Task AnalyzeAsync_スタックトレースが4000文字を超える場合_切り捨てられる()
    {
        var longTrace = new string('x', 5000);
        var ex        = new FakeException("overflow", longTrace);
        var handler   = new FakeHttpMessageHandler(HttpStatusCode.OK, ValidClaudeResponse());
        var analyzer  = CreateAnalyzer(handler);

        await analyzer.AnalyzeAsync(BuildContext(ex));

        // リクエストボディに "切り捨て" の文字列が含まれること（4000 文字で切り捨て済み）
        var requestBody = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("切り捨て", requestBody);
        // 元の 5000 文字の 'x' が全部含まれていないこと
        Assert.DoesNotContain(new string('x', 4001), requestBody);
    }
}
