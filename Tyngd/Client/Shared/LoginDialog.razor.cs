using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Tyngd.Client.Shared;

public partial class LoginDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private AuthService AuthService { get; set; } = default!;

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

        var (user, error) = await AuthService.LoginAsync(new LoginRequest
        {
            UserName = _username.Trim(),
            Password = _password,
        });

        if (user is null)
        {
            _errorMessage = error ?? "Felaktigt användarnamn eller lösenord.";
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
}
