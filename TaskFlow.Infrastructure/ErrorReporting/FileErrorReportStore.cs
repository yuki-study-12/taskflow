using System.Text;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Infrastructure.ErrorReporting;

internal sealed class FileErrorReportStore(
    IOptions<ErrorAnalysisSettings> options)
    : IErrorReportStore
{
    public async Task<string> SaveAsync(
        ExceptionContext  context,
        ErrorAnalysis     analysis,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return string.Empty;

        var settings      = options.Value;
        var exceptionType = SanitizeFileName(context.Exception.GetType().Name);
        var dateFolder    = context.OccurredAt.ToString("yyyy-MM-dd");
        var timestamp     = context.OccurredAt.ToString("yyyyMMddHHmmss");
        var fileName      = $"{timestamp}_{exceptionType}.md";
        var directory     = Path.Combine(settings.LogDirectory, dateFolder);
        var filePath      = Path.Combine(directory, fileName);

        Directory.CreateDirectory(directory);

        var markdown = BuildMarkdown(context, analysis);
        await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8, cancellationToken);

        return Path.GetFullPath(filePath);
    }

    private static string BuildMarkdown(ExceptionContext context, ErrorAnalysis analysis)
    {
        var exceptionType = context.Exception.GetType().Name;
        var stackTrace    = context.Exception.StackTrace ?? "(スタックトレースなし)";

        var affectedFiles = analysis.AffectedFiles.Count > 0
            ? string.Join("\n", analysis.AffectedFiles.Select(f => $"- `{f}`"))
            : "- (特定できませんでした)";

        return $"""
            # Error Report: {exceptionType} at {context.OccurredAt:O}

            ## Exception
            - **Type**: `{context.Exception.GetType().FullName}`
            - **Message**: {context.Exception.Message}
            - **Endpoint**: `{context.Endpoint}`
            - **OccurredAt**: {context.OccurredAt:O}
            - **Environment**: {context.Environment}
            - **UserId**: {context.UserId ?? "(anonymous)"}

            ## Stack Trace
            ```
            {stackTrace}
            ```

            ## AI Analysis

            ### Root Cause
            {analysis.RootCause}

            ### Affected Files
            {affectedFiles}

            ### Impact
            {analysis.Impact}

            ### Recommended Fix
            {analysis.RecommendedFix}
            """;
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c =>
            char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
}
