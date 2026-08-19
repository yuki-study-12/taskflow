using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// 例外情報を AI で解析し、エラー解析結果を返すサービス。
/// </summary>
public interface IExceptionAnalyzer
{
    Task<ErrorAnalysis> AnalyzeAsync(
        ExceptionContext  context,
        CancellationToken cancellationToken = default);
}
