namespace TaskFlow.Application.Common.Models;

/// <summary>
/// 例外発生時のコンテキスト情報スナップショット。
/// HttpContext への参照を持たないため、レスポンス送信後も安全に参照できる。
/// </summary>
public sealed record ExceptionContext(
    Exception      Exception,
    string         Endpoint,
    string?        UserId,
    string         Environment,
    DateTimeOffset OccurredAt);
