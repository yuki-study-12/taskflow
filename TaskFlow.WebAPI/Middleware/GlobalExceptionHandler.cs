using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.WebAPI.Middleware;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment             env,
    IExceptionAnalyzer              analyzer,
    IErrorReportStore               store,
    IErrorNotificationService       notifier)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException      => (StatusCodes.Status400BadRequest,        "Validation Error"),
            NotFoundException        => (StatusCodes.Status404NotFound,           "Not Found"),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden,          "Forbidden"),
            _                        => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred");

            // 例外コンテキストのスナップショットを同期的に取得（HttpContext への参照は持たない）
            var ctx = new ExceptionContext(
                exception,
                $"{httpContext.Request.Method} {httpContext.Request.Path}",
                httpContext.User.FindFirst("sub")?.Value,
                env.EnvironmentName,
                DateTimeOffset.UtcNow);

            // fire-and-forget — HTTP レスポンスをブロックしない
            // 内部の try-catch により未観察タスク例外によるプロセスクラッシュを防ぐ
            _ = Task.Run(async () =>
            {
                try
                {
                    var analysis = await analyzer.AnalyzeAsync(ctx);
                    var path     = await store.SaveAsync(ctx, analysis);
                    await notifier.SendAsync(ctx, path);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "エラー分析パイプラインが失敗しました (type={ExceptionType})",
                        ctx.Exception.GetType().Name);
                }
            });
        }

        httpContext.Response.StatusCode = statusCode;

        if (exception is ValidationException validationEx)
        {
            var problem = new ValidationProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Type   = $"https://httpstatuses.io/{statusCode}",
            };
            foreach (var (key, messages) in validationEx.Errors)
                problem.Errors[key] = messages;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }
        else
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = exception.Message,
                Type   = $"https://httpstatuses.io/{statusCode}",
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }
}
