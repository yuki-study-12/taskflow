using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// エラー発生をメールで担当者に通知するサービス。
/// </summary>
public interface IErrorNotificationService
{
    Task SendAsync(
        ExceptionContext  context,
        string            reportFilePath,
        CancellationToken cancellationToken = default);
}
