namespace ArnoldOne.Server.Services;

public interface IErrorNotificationService
{
    Task NotifyAsync(ErrorNotificationMessage message, CancellationToken cancellationToken = default);
}
