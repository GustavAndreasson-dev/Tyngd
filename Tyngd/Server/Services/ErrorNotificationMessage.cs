namespace Tyngd.Server.Services;

public sealed class ErrorNotificationMessage
{
    public string Source { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Details { get; set; } = new Dictionary<string, string>();

    public string Fingerprint =>
        string.Join('|', Source, ExceptionType, Message, Path).ToUpperInvariant();
}
