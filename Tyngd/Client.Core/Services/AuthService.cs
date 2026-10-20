using Tyngd.Shared.DTOs;
using Tyngd.Shared.Users;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

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

    /// <summary>Raderar (avidentifierar) kontot och loggar ut. Returnerar ett felmeddelande, eller null om det gick.</summary>
    public async Task<string?> DeleteAccountAsync(string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/delete-account", new DeleteAccountRequest { Password = password });
        if (!response.IsSuccessStatusCode)
            return response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? await response.Content.ReadAsStringAsync()
                : HttpErrorText.For(new HttpRequestException(null, null, response.StatusCode));

        await _loginState.LogoutAsync();
        return null;
    }

    /// <summary>Allt som sparats om användaren, som JSON-fil (filnamn och innehåll).</summary>
    public async Task<(string FileName, byte[] Content)> ExportMyDataAsync()
    {
        var response = await _http.GetAsync("api/account/export");
        response.EnsureSuccessStatusCode();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "tyngd-data.json";
        return (fileName, await response.Content.ReadAsByteArrayAsync());
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
