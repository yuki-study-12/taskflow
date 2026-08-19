using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.Infrastructure.ErrorAnalysis;

internal sealed class SmtpErrorNotificationService(
    IOptions<ErrorAnalysisSettings> options)
    : IErrorNotificationService
{
    public async Task SendAsync(
        ExceptionContext  context,
        string            reportFilePath,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (!settings.Enabled
            || string.IsNullOrWhiteSpace(settings.SmtpHost)
            || string.IsNullOrWhiteSpace(settings.NotificationEmail))
            return;

        var exceptionType = context.Exception.GetType().Name;
        var subject       = $"[TaskFlow] Unhandled Exception: {exceptionType} on {context.Endpoint}";
        var body          = $"""
            TaskFlow で未処理の例外が発生しました。

            発生日時   : {context.OccurredAt:O}
            例外タイプ : {context.Exception.GetType().FullName}
            メッセージ : {context.Exception.Message}
            エンドポイント: {context.Endpoint}
            ユーザーID : {context.UserId ?? "(anonymous)"}
            環境       : {context.Environment}

            詳細な解析レポートは以下のファイルを参照してください:
            {(string.IsNullOrEmpty(reportFilePath) ? "(保存なし)" : reportFilePath)}

            このレポートを AI エージェントに読み込ませることで自動修正の提案が得られます。
            """;

        // TODO: .NET 5+ では SmtpClient は Obsolete。MailKit への移行を検討する。
#pragma warning disable SYSLIB0006
        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl   = true,
            Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword)
        };

        using var message = new MailMessage(
            settings.FromEmail,
            settings.NotificationEmail,
            subject,
            body);

        await client.SendMailAsync(message, cancellationToken);
#pragma warning restore SYSLIB0006
    }
}
