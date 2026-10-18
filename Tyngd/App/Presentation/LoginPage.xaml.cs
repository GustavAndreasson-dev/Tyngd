using Tyngd.App.Presentation.Dialogs;

namespace Tyngd.App.Presentation;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        this.InitializeComponent();
    }

    private async void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
            return;

        var dialog = new RegisterDialog(vm.AuthService) { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();

        if (dialog.User is not null)
            await vm.GoToMainAsync();
    }
}
