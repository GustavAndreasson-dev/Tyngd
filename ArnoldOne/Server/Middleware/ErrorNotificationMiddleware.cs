using ArnoldOne.Server.Services;
using System.Security.Claims;

namespace ArnoldOne.Server.Middleware;

public sealed class ErrorNotificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorNotificationMiddleware> _logger;

    public ErrorNotificationMiddleware(RequestDelegate next, ILogger<ErrorNotificationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IErrorNotificationService errorNotificationService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var message = new ErrorNotificationMessage
            {
                Source = "Server",
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.ToString(),
                Path = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}",
                UserId = GetUserId(context),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RequestId = context.TraceIdentifier,
                OccurredAt = DateTimeOffset.UtcNow,
            };

            try
            {
                await errorNotificationService.NotifyAsync(message, context.RequestAborted);
            }
            catch (Exception notificationException)
            {
                _logger.LogError(notificationException, "Failed while reporting server exception.");
            }

            throw;
        }
    }

    private static string? GetUserId(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.User.FindFirstValue("sub");
}
