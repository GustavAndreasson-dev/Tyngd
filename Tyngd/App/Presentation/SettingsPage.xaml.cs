namespace Tyngd.App.Presentation;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        this.KeepFocusedInputVisible();
    }

    private async void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
            return;

        // Utloggning raderar lokala pass; fråga först om något inte kunde synkas (t.ex. offline).
        if (await vm.HasUnsyncedPassesAfterSyncAsync())
        {
            var dialog = new ContentDialog
            {
                Title = "Osynkade pass",
                Content = "Du har pass som inte har synkats till servern. De raderas från telefonen om du loggar ut. Logga ut ändå?",
                PrimaryButtonText = "Logga ut ändå",
                CloseButtonText = "Avbryt",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;
        }

        await vm.LogoutAsync();
    }
}
