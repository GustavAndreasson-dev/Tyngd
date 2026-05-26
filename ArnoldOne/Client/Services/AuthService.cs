using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.Users;
using System.Net.Http.Json;

namespace ArnoldOne.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly LoginState _loginState;

    public AuthService(HttpClient http, LoginState loginState)
    {
        _http       = http;
        _loginState = loginState;
    }

    public async Task<(UserInfo? User, string? Error)> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is not null)
            {
                await _loginState.LoginAsync(result.User, result.Token);
                return (result.User, null);
            }
        }

        var error = await response.Content.ReadAsStringAsync();
        return (null, error);
    }

    public async Task<(UserInfo? User, string? Error)> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is not null)
            {
                await _loginState.LoginAsync(result.User, result.Token);
                return (result.User, null);
            }
        }

        var error = await response.Content.ReadAsStringAsync();
        return (null, error);
    }
}
