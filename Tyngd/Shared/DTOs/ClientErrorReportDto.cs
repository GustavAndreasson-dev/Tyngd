namespace Tyngd.Shared.DTOs;

public sealed class ClientErrorReportDto
{
    public SerializableExceptionDto Exception { get; set; } = new();
    public string Uri { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
