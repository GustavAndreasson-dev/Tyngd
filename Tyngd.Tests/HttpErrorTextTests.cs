using System.Net;
using Tyngd.Client.Services;

namespace Tyngd.Tests;

public class HttpErrorTextTests
{
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "Serverfel (500).")]
    [InlineData(HttpStatusCode.Conflict, "Krock med en annan sparning, försöker igen senare.")]
    [InlineData(HttpStatusCode.Forbidden, "Du har inte behörighet.")]
    [InlineData(HttpStatusCode.BadRequest, "Felkod 400.")]
    public void Maps_status_codes_to_swedish(HttpStatusCode status, string expected) =>
        Assert.Equal(expected, HttpErrorText.For(new HttpRequestException("net_http_message_not_success_statuscode_reason", null, status)));

    [Fact]
    public void No_status_means_no_contact() =>
        Assert.Equal("Ingen kontakt med servern.", HttpErrorText.For(new HttpRequestException("x")));
}
