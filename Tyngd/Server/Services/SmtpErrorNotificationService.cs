using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Tyngd.Server.Services;

public sealed class SmtpErrorNotificationService : IErrorNotificationService
{
    private readonly IOptionsMonitor<ErrorNotificationOptions> _options;
    private readonly ILogger<SmtpErrorNotificationService> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSentByFingerprint = new();
    // One window per source, so anonymous client reports cannot use up the emails for server errors.
    private readonly Dictionary<string, (DateTimeOffset Start, int Sent)> _capWindows = new();

    public SmtpErrorNotificationService(
        IOptionsMonitor<ErrorNotificationOptions> options,
        ILogger<SmtpErrorNotificationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task NotifyAsync(ErrorNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
            return;

        var recipients = options.To
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (recipients.Length == 0 || string.IsNullOrWhiteSpace(options.Smtp.Host) || string.IsNullOrWhiteSpace(options.Smtp.From))
        {
            _logger.LogWarning("Error notifications are enabled but SMTP settings are incomplete.");
            return;
        }

        if (IsThrottled(message, options.ThrottleMinutes) || IsOverHourlyCap(message.Source, options.MaxPerHour))
            return;

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(options.Smtp.From),
            Subject = BuildSubject(message),
            Body = BuildBody(message),
            IsBodyHtml = true,
        };

        foreach (var recipient in recipients)
            mailMessage.To.Add(recipient);

        using var smtpClient = new SmtpClient(options.Smtp.Host, options.Smtp.Port)
        {
            EnableSsl = options.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(options.Smtp.UserName))
            smtpClient.Credentials = new NetworkCredential(options.Smtp.UserName, options.Smtp.Password);

        try
        {
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error notification email.");
        }
    }

    private bool IsThrottled(ErrorNotificationMessage message, int throttleMinutes)
    {
        if (throttleMinutes <= 0)
            return false;

        var now = DateTimeOffset.UtcNow;
        var fingerprint = message.Fingerprint;

        if (_lastSentByFingerprint.TryGetValue(fingerprint, out var lastSent) &&
            now - lastSent < TimeSpan.FromMinutes(throttleMinutes))
        {
            return true;
        }

        // Unique messages would otherwise grow this dictionary forever.
        if (_lastSentByFingerprint.Count >= MaxTrackedFingerprints)
        {
            var cutoff = now - TimeSpan.FromMinutes(throttleMinutes);
            foreach (var entry in _lastSentByFingerprint.Where(e => e.Value < cutoff))
                _lastSentByFingerprint.TryRemove(entry.Key, out _);
            if (_lastSentByFingerprint.Count >= MaxTrackedFingerprints)
                _lastSentByFingerprint.Clear();
        }

        _lastSentByFingerprint[fingerprint] = now;
        return false;
    }

    private const int MaxTrackedFingerprints = 1000;

    private bool IsOverHourlyCap(string source, int maxPerHour)
    {
        if (maxPerHour <= 0)
            return false;

        lock (_capWindows)
        {
            var now = DateTimeOffset.UtcNow;
            var window = _capWindows.GetValueOrDefault(source, (Start: DateTimeOffset.MinValue, Sent: 0));
            if (now - window.Start >= TimeSpan.FromHours(1))
                window = (now, 0);

            if (window.Sent >= maxPerHour)
                return true;

            _capWindows[source] = (window.Start, window.Sent + 1);
            return false;
        }
    }

    private static string BuildSubject(ErrorNotificationMessage message)
    {
        // ExceptionType comes from anonymous clients; CR/LF in a subject would throw.
        var subject = $"Tyngd - {message.Source} error - {message.ExceptionType}".ReplaceLineEndings(" ");
        return subject.Length <= 180 ? subject : subject[..180];
    }

    private static string BuildBody(ErrorNotificationMessage message)
    {
        var body = new StringBuilder();
        body.AppendLine("<html><body>");
        body.AppendLine("<h2>Tyngd error</h2>");
        AppendRow(body, "Source", message.Source);
        AppendRow(body, "Time UTC", message.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AppendRow(body, "Path/URL", message.Path);
        AppendRow(body, "User ID", message.UserId ?? string.Empty);
        AppendRow(body, "Request ID", message.RequestId ?? string.Empty);
        AppendRow(body, "User agent", message.UserAgent ?? string.Empty);
        AppendRow(body, "Exception type", message.ExceptionType);
        AppendRow(body, "Message", message.Message);

        foreach (var detail in message.Details)
            AppendRow(body, detail.Key, detail.Value);

        body.AppendLine("<h3>Stack trace</h3>");
        body.Append("<pre>");
        body.Append(WebUtility.HtmlEncode(message.StackTrace));
        body.AppendLine("</pre>");
        body.AppendLine("</body></html>");
        return body.ToString();
    }

    private static void AppendRow(StringBuilder body, string label, string value)
    {
        body.Append("<p><strong>");
        body.Append(WebUtility.HtmlEncode(label));
        body.Append(":</strong> ");
        body.Append(WebUtility.HtmlEncode(value));
        body.AppendLine("</p>");
    }
}
