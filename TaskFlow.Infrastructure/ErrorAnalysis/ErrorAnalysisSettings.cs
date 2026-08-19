namespace TaskFlow.Infrastructure.ErrorAnalysis;

public sealed class ErrorAnalysisSettings
{
    public const string SectionName = "ErrorAnalysis";

    /// <summary>
    /// false にするとパイプライン全体をスキップする（ローカル開発向け）。
    /// </summary>
    public bool Enabled { get; init; } = true;

    // ── Claude API ──────────────────────────────────────────────────────
    public string ClaudeApiKey    { get; init; } = string.Empty;
    public string ClaudeModel     { get; init; } = "claude-sonnet-4-6";
    public int    ClaudeMaxTokens { get; init; } = 1024;

    // ── MD ファイル保存先 ────────────────────────────────────────────────
    /// <summary>logs/errors/{yyyy-MM-dd}/ の親ディレクトリ。</summary>
    public string LogDirectory { get; init; } = "logs/errors";

    // ── SMTP ─────────────────────────────────────────────────────────────
    public string SmtpHost          { get; init; } = string.Empty;
    public int    SmtpPort          { get; init; } = 587;
    public string SmtpUser          { get; init; } = string.Empty;
    public string SmtpPassword      { get; init; } = string.Empty;
    public string NotificationEmail { get; init; } = string.Empty;
    public string FromEmail         { get; init; } = "noreply@taskflow.app";
}
