using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// エラー解析結果を Markdown ファイルとして永続化するサービス。
/// 保存したファイルの絶対パスを返す。
/// </summary>
public interface IErrorReportStore
{
    Task<string> SaveAsync(
        ExceptionContext  context,
        ErrorAnalysis     analysis,
        CancellationToken cancellationToken = default);
}
