namespace TaskFlow.Application.Common.Models;

/// <summary>
/// AIエージェントによるエラー解析結果。
/// </summary>
public sealed record ErrorAnalysis(
    string               RootCause,
    IReadOnlyList<string> AffectedFiles,
    string               Impact,
    string               RecommendedFix)
{
    /// <summary>解析が無効または不要な場合に返す空の解析結果。</summary>
    public static ErrorAnalysis Empty { get; } =
        new("Analysis disabled", [], string.Empty, string.Empty);
}
