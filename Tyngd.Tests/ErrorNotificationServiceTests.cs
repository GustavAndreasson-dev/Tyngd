using System.Net;
using System.Net.Sockets;
using Tyngd.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tyngd.Tests;

public class ErrorNotificationServiceTests
{
    [Fact]
    public async Task Unique_messages_are_capped_per_hour()
    {
        var connections = await CountSmtpConnectionsAsync(Enumerable.Range(0, 10).Select(i => ("Client", $"unique {i}")));

        Assert.Equal(3, connections);
    }

    [Fact]
    public async Task Client_reports_cannot_use_up_the_cap_for_server_errors()
    {
        var connections = await CountSmtpConnectionsAsync(
            Enumerable.Range(0, 10).Select(i => ("Client", $"unique {i}")).Append(("Server", "crash")));

        Assert.Equal(4, connections);
    }

    /// <summary>Sends the messages with a cap of 3 per hour and counts how many reached the SMTP server.</summary>
    private static async Task<int> CountSmtpConnectionsAsync(IEnumerable<(string Source, string Message)> messages)
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

        foreach (var (source, message) in messages)
            await service.NotifyAsync(new ErrorNotificationMessage { Source = source, ExceptionType = "E", Message = message },
                TestContext.Current.CancellationToken);
        listener.Stop();

        return connections;
    }

    private sealed class StaticOptions(ErrorNotificationOptions value) : IOptionsMonitor<ErrorNotificationOptions>
    {
        public ErrorNotificationOptions CurrentValue => value;
        public ErrorNotificationOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<ErrorNotificationOptions, string?> listener) => null;
    }
}
