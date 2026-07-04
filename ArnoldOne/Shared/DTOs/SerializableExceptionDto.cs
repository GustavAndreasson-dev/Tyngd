namespace ArnoldOne.Shared.DTOs;

public sealed class SerializableExceptionDto
{
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public SerializableExceptionDto? InnerException { get; set; }

    public static SerializableExceptionDto FromException(Exception exception) =>
        new()
        {
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            Source = exception.Source ?? string.Empty,
            StackTrace = exception.StackTrace ?? string.Empty,
            InnerException = exception.InnerException is null
                ? null
                : FromException(exception.InnerException),
        };
}
