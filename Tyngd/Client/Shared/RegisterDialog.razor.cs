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

        if (!IsValidEmail(_email.Trim()))
        {
            _errorMessage = "Ange en giltig e-postadress.";
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

    private static bool IsValidEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        var dotIndex = email.IndexOf('.', atIndex);
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }}
