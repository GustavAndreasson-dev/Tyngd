using System.Net;
using System.Text.Json;
using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class ClientErrorServiceTests
{
    [Fact]
    public async Task Sends_the_exception_and_location()
    {
        string? body = null;
        var service = Service(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await service.ReportAsync(new InvalidOperationException("trasigt"), "app");

        var report = JsonSerializer.Deserialize<ClientErrorReportDto>(body!, JsonSerializerOptions.Web)!;
        Assert.Equal(("app", "trasigt"), (report.Uri, report.Exception.Message));
    }

    [Fact]
    public async Task Never_throws_when_the_server_is_unreachable() =>
        await Service(_ => throw new HttpRequestException()).ReportAsync(new Exception(), null);

    private static ClientErrorService Service(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://localhost/") });

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
