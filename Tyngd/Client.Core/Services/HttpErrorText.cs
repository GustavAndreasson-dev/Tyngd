using System.Net;

namespace Tyngd.Client.Services;

/// <summary>
/// Svensk text för ett misslyckat API-anrop. HttpRequestException.Message är en resursnyckel i trimmade
/// WebAssembly-byggen ("net_http_message_not_success_statuscode_reason"), så den visas aldrig.
/// </summary>
public static class HttpErrorText
{
    public static string For(HttpRequestException ex) => ex.StatusCode switch
    {
        null                              => "Ingen kontakt med servern.",
        HttpStatusCode.Unauthorized       => "Du är inte inloggad.",
        HttpStatusCode.Forbidden          => "Du har inte behörighet.",
        HttpStatusCode.NotFound           => "Resursen hittades inte.",
        HttpStatusCode.Conflict           => "Krock med en annan sparning, försöker igen senare.",
        HttpStatusCode.ServiceUnavailable => "Servern är inte tillgänglig.",
        >= HttpStatusCode.InternalServerError => $"Serverfel ({(int)ex.StatusCode}).",
        _                                 => $"Felkod {(int)ex.StatusCode}.",
    };
}
