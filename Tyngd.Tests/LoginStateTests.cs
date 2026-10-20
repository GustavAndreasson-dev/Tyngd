using System.Net;
using Tyngd.Client.Services;
using Tyngd.Shared.Users;

namespace Tyngd.Tests;

public class LoginStateTests
{
    [Fact]
    public async Task Expired_token_logs_out_but_keeps_unsynced_pass_data()
    {
        var (storage, loginState) = await LoggedInAsync();
        storage.Items["pass-1-2"] = "{\"IsSynced\":false}";

        using var http = new HttpClient(new AuthTokenHandler(loginState) { InnerHandler = new StatusHandler(HttpStatusCode.Unauthorized) });
        await http.GetAsync("http://localhost/api/Dashboard");

        Assert.False(loginState.IsLoggedIn);
        Assert.False(storage.Items.ContainsKey("current-token"));
        Assert.True(storage.Items.ContainsKey("pass-1-2"));
    }

    [Fact]
    public async Task Pressing_log_out_clears_pass_data()
    {
        var (storage, loginState) = await LoggedInAsync();
        storage.Items["pass-1-2"] = "{\"IsSynced\":false}";

        await loginState.LogoutAsync();

        Assert.False(storage.Items.ContainsKey("pass-1-2"));
    }

    private static async Task<(FakeLocalStorage, LoginState)> LoggedInAsync()
    {
        var storage = new FakeLocalStorage();
        var loginState = new LoginState(new BlazoredAppStorage(storage), new SessionState());
        await loginState.LoginAsync(new UserInfo { UserName = "anna" }, "token");
        return (storage, loginState);
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
