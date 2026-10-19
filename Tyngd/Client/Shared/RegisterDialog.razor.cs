using Tyngd.Client.Services;
using Tyngd.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Tyngd.Client.Shared;

public partial class RegisterDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private AuthService AuthService { get; set; } = default!;

    private string _username        = string.Empty;
    private string _email           = string.Empty;
    private string _password        = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorMessage;

    private async Task Submit()
    {
        _errorMessage = RegisterRequestValidator.Validate(_username, _email, _password, _confirmPassword);
        if (_errorMessage is not null)
            return;

        var (user, error) = await AuthService.RegisterAsync(new RegisterRequest
        {
            UserName = _username.Trim(),
            Email    = _email.Trim(),
            Password = _password,
        });

        if (user is null)
        {
            _errorMessage = error ?? "Registreringen misslyckades.";
            return;
        }

        MudDialog.Close(DialogResult.Ok(user));
    }

    private void Cancel() => MudDialog.Cancel();
}
