using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly INavigator _navigator;

    public LoginViewModel(AuthService authService, INavigator navigator)
    {
        _authService = authService;
        _navigator = navigator;
    }

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Fyll i användarnamn och lösenord.";
            return;
        }

        try
        {
            var (user, error) = await _authService.LoginAsync(new LoginRequest
            {
                UserName = UserName.Trim(),
                Password = Password,
            });

            if (user is null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Felaktigt användarnamn eller lösenord." : error;
                return;
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Kunde inte nå servern. Kontrollera anslutningen.";
            return;
        }

        Password = string.Empty;
        await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
}
