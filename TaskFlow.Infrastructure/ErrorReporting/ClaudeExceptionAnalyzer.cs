using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Infrastructure.ErrorReporting;

public sealed class ClaudeExceptionAnalyzer(
    IHttpClientFactory                  httpFactory,
    IOptions<ErrorAnalysisSettings>     options)
    : IExceptionAnalyzer
{
    private const int MaxStackTraceLength = 4000;

    public async Task<ErrorAnalysis> AnalyzeAsync(
        ExceptionContext  context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return ErrorAnalysis.Empty;

        var settings   = options.Value;
        var stackTrace = context.Exception.StackTrace ?? string.Empty;
        if (stackTrace.Length > MaxStackTraceLength)
            stackTrace = stackTrace[..MaxStackTraceLength] + "\n... (切り捨て)";

        var prompt = $"""
            You are an expert C# / ASP.NET Core developer.
            Analyze the following exception and respond ONLY with a JSON object
            with exactly these keys:
              "rootCause"     : string — 原因の説明
              "affectedFiles" : string[] — 影響するファイルパスと行番号 (例: "Foo/Bar.cs (line 42)")
              "impact"        : string — 影響範囲
              "recommendedFix": string — 具体的な修正方針

            Exception Type : {context.Exception.GetType().FullName}
            Message        : {context.Exception.Message}
            Endpoint       : {context.Endpoint}
            UserId         : {context.UserId ?? "(anonymous)"}
            Environment    : {context.Environment}

            Stack Trace:
            {stackTrace}
            """;

        var requestBody = new
        {
            model      = settings.ClaudeModel,
            max_tokens = settings.ClaudeMaxTokens,
            messages   = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var http     = httpFactory.CreateClient("claude");
        var response = await http.PostAsJsonAsync(
            "/v1/messages", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc    = JsonDocument.Parse(responseBody);

        // Claude レスポンス: { "content": [{ "type": "text", "text": "{...json...}" }] }
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        // text 中の JSON ブロックを抽出（```json ... ``` があれば除去）
        var jsonText = ExtractJson(text);

        var dto = JsonSerializer.Deserialize<AnalysisDto>(jsonText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Claude レスポンスを解析できませんでした");

        return new ErrorAnalysis(
            dto.RootCause,
            dto.AffectedFiles ?? [],
            dto.Impact,
            dto.RecommendedFix);
    }

    private static string ExtractJson(string text)
    {
        // ```json ... ``` または ``` ... ``` のコードフェンスを除去
        var start = text.IndexOf('{');
        var end   = text.LastIndexOf('}');
        return start >= 0 && end > start
            ? text[start..(end + 1)]
            : text;
    }

    private sealed class AnalysisDto
    {
        [JsonPropertyName("rootCause")]
        public string RootCause { get; init; } = string.Empty;

        [JsonPropertyName("affectedFiles")]
        public string[]? AffectedFiles { get; init; }

        [JsonPropertyName("impact")]
        public string Impact { get; init; } = string.Empty;

        [JsonPropertyName("recommendedFix")]
        public string RecommendedFix { get; init; } = string.Empty;
    }
}
