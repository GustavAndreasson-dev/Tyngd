using System.Net;
using System.Net.Http.Headers;

namespace ArnoldOne.Client.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly LoginState _loginState;

    public AuthTokenHandler(LoginState loginState)
    {
        _loginState = loginState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_loginState.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _loginState.Token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && _loginState.IsLoggedIn)
            await _loginState.LogoutAsync();

        return response;
    }
}
