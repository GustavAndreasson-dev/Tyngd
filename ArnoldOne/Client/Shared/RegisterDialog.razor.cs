using ArnoldOne.Shared.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace ArnoldOne.Client.Shared;

/// <summary>
/// Registreringsdialog. Utför för närvarande en mock-registrering.
/// Ersätt <see cref="MockRegisterAsync"/> med ett riktigt API-anrop mot /api/auth/register när backend är redo.
/// </summary>
public partial class RegisterDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private string _username        = string.Empty;
    private string _email           = string.Empty;
    private string _password        = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorMessage;

    private async Task Submit()
    {
        _errorMessage = null;

        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_email) ||
            string.IsNullOrWhiteSpace(_password) || string.IsNullOrWhiteSpace(_confirmPassword))
        {
            _errorMessage = "Fyll i alla fält.";
            return;
        }

        if (_username.Trim().Length < 3)
        {
            _errorMessage = "Användarnamnet måste vara minst 3 tecken.";
            return;
        }

        if (LoginDialog.MockUsers.ContainsKey(_username.Trim()))
        {
            _errorMessage = "Användarnamnet är redan taget.";
            return;
        }

        if (!IsValidEmail(_email.Trim()))
        {
            _errorMessage = "Ange en giltig e-postadress.";
            return;
        }

        if (LoginDialog.MockUsers.Values.Any(u => u.Email.Equals(_email.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            _errorMessage = "E-postadressen är redan registrerad.";
            return;
        }

        if (_password.Length < 6)
        {
            _errorMessage = "Lösenordet måste vara minst 6 tecken.";
            return;
        }

        if (_password != _confirmPassword)
        {
            _errorMessage = "Lösenorden matchar inte.";
            return;
        }

        var user = await MockRegisterAsync(_username.Trim(), _email.Trim());
        MudDialog.Close(DialogResult.Ok(user));
    }

    private void Cancel() => MudDialog.Cancel();

    private static bool IsValidEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        var dotIndex = email.IndexOf('.', atIndex);
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await Submit();
    }

    /// <summary>
    /// Mock-registrering – skapar alltid ett nytt konto med rollen User.
    /// Ersätts med HttpClient-anrop mot /api/auth/register när backend finns.
    /// </summary>
    private static Task<UserInfo> MockRegisterAsync(string username, string email)
    {
        return Task.FromResult(new UserInfo
        {
            Id        = Guid.NewGuid().ToString(),
            UserName  = username,
            Email     = email,
            CreatedAt = DateTime.UtcNow,
            Roles     = [AppRoles.User],
        });
    }
}
