using Tyngd.Shared.DTOs;
using Tyngd.Shared.Users;

namespace Tyngd.App.Presentation.Dialogs;

/// <summary>Skapar ett konto, som RegisterDialog i webbappen. Lyckad registrering loggar in användaren (via AuthService).</summary>
public sealed partial class RegisterDialog : ContentDialog
{
    private readonly AuthService _authService;

    public RegisterDialog(AuthService authService)
    {
        _authService = authService;
        this.InitializeComponent();
    }

    /// <summary>Den registrerade (och inloggade) användaren, eller null om dialogen avbröts.</summary>
    public UserInfo? User { get; private set; }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        IsPrimaryButtonEnabled = false;
        try
        {
            ErrorText.Text = await SubmitAsync();
            // Dialogen ska bara stängas vid lyckad registrering.
            args.Cancel = User is null;
        }
        finally
        {
            IsPrimaryButtonEnabled = true;
            deferral.Complete();
        }
    }

    /// <summary>Validerar som webbappen och registrerar. Returnerar felmeddelandet, eller null vid lyckat resultat.</summary>
    private async Task<string?> SubmitAsync()
    {
        var userName = UserNameBox.Text;
        var email = EmailBox.Text;
        var password = PasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        if (RegisterRequestValidator.Validate(userName, email, password, confirmPassword) is { } validationError)
            return validationError;

        try
        {
            var (user, error) = await _authService.RegisterAsync(new RegisterRequest
            {
                UserName = userName.Trim(),
                Email    = email.Trim(),
                Password = password,
            });

            User = user;
            return user is null ? (string.IsNullOrWhiteSpace(error) ? "Registreringen misslyckades." : error) : null;
        }
        catch (HttpRequestException)
        {
            return "Kunde inte nå servern. Kontrollera anslutningen.";
        }
    }
}
