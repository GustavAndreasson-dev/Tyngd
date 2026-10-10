using System.Net;
using System.Net.Sockets;
using ArnoldOne.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ArnoldOne.Tests;

public class ErrorNotificationServiceTests
{
    [Fact]
    public async Task Unique_messages_are_capped_per_hour()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var connections = 0;
        var accept = Task.Run(async () =>
        {
            while (true)
            {
                using var client = await listener.AcceptTcpClientAsync();
                Interlocked.Increment(ref connections); // close at once: the send fails, which the service logs
            }
        });

        var service = new SmtpErrorNotificationService(
            new StaticOptions(new ErrorNotificationOptions
            {
                Enabled = true,
                To = ["to@example.com"],
                MaxPerHour = 3,
                Smtp = { Host = "127.0.0.1", Port = port, EnableSsl = false, From = "from@example.com" },
            }),
            NullLogger<SmtpErrorNotificationService>.Instance);

        for (var i = 0; i < 10; i++)
            await service.NotifyAsync(new ErrorNotificationMessage { Source = "Client", ExceptionType = "E", Message = $"unique {i}" },
                TestContext.Current.CancellationToken);
        listener.Stop();

        Assert.Equal(3, connections);
    }

    private sealed class StaticOptions(ErrorNotificationOptions value) : IOptionsMonitor<ErrorNotificationOptions>
    {
        public ErrorNotificationOptions CurrentValue => value;
        public ErrorNotificationOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<ErrorNotificationOptions, string?> listener) => null;
    }
}
