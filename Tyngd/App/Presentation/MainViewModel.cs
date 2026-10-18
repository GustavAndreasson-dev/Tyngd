namespace Tyngd.App.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly LoginState _loginState;
    private readonly INavigator _navigator;

    public MainViewModel(LoginState loginState, INavigator navigator)
    {
        _loginState = loginState;
        _navigator = navigator;
    }

    public string Greeting => $"Inloggad som {_loginState.CurrentUser?.UserName}";

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _loginState.LogoutAsync();
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
}
