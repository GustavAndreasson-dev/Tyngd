using Tyngd.Server.Services;
using Tyngd.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Tyngd.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/client-errors")]
[RequestSizeLimit(16_384)]
[EnableRateLimiting("client-errors")]
public class ClientErrorsController : ControllerBase
{
    private readonly IErrorNotificationService _errorNotificationService;

    public ClientErrorsController(IErrorNotificationService errorNotificationService)
    {
        _errorNotificationService = errorNotificationService;
    }

    [HttpPost]
    public async Task<IActionResult> Report(ClientErrorReportDto report)
    {
        if (string.IsNullOrWhiteSpace(report.Exception.Message))
            return BadRequest("Exception message is required.");

        var message = new ErrorNotificationMessage
        {
            Source = "Client",
            ExceptionType = report.Exception.ExceptionType,
            Message = report.Exception.Message,
            StackTrace = FormatException(report.Exception),
            Path = report.Uri,
            UserId = GetUserId(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            RequestId = HttpContext.TraceIdentifier,
            OccurredAt = report.OccurredAt,
            Details = new Dictionary<string, string>
            {
                ["ReportedAtUtc"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            },
        };

        await _errorNotificationService.NotifyAsync(message, HttpContext.RequestAborted);
        return Accepted();
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private static string FormatException(SerializableExceptionDto exception)
    {
        var exceptions = new List<SerializableExceptionDto>();
        for (var current = exception; current is not null; current = current.InnerException)
            exceptions.Add(current);

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            exceptions.Select(current =>
                $"{current.ExceptionType}: {current.Message}{Environment.NewLine}{current.StackTrace}"));
    }
}
