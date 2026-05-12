using ArnoldOne.Shared.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Shared;

/// <summary>
/// Inloggningsdialog. Utför för närvarande en mock-inloggning.
/// Ersätt <see cref="MockLoginAsync"/> med ett riktigt API-anrop när backend är redo.
/// </summary>
public partial class LoginDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;

    private async Task Submit()
    {
        _errorMessage = null;

        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            _errorMessage = "Fyll i användarnamn och lösenord.";
            return;
        }

        var user = await MockLoginAsync(_username, _password);

        if (user is null)
        {
            _errorMessage = "Felaktigt användarnamn eller lösenord.";
            return;
        }

        MudDialog.Close(DialogResult.Ok(user));
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await Submit();
    }

    /// <summary>
    /// Mock-användare – ersätts med HttpClient-anrop mot /api/auth/login när backend finns.
    /// </summary>
    internal static readonly Dictionary<string, UserInfo> MockUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gustav"] = new() { Id = "a1b2c3d4-0000-0000-0000-000000000001", UserName = "Gustav", Email = "gustav@example.com", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Roles = [AppRoles.Admin] },
        ["Ulf"]    = new() { Id = "a1b2c3d4-0000-0000-0000-000000000002", UserName = "Ulf",    Email = "ulf@example.com",    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Roles = [AppRoles.Admin] },
    };

    private const string MockPassword = "Pytten";

    private static Task<UserInfo?> MockLoginAsync(string username, string password)
    {
        if (password != MockPassword)
            return Task.FromResult<UserInfo?>(null);

        var user = MockUsers.Values.FirstOrDefault(u =>
            u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Equals(username, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }
}
