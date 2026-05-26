using System.Net.Http.Headers;

namespace ArnoldOne.Client.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly LoginState _loginState;

    public AuthTokenHandler(LoginState loginState)
    {
        _loginState = loginState;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_loginState.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _loginState.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
