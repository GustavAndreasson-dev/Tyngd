using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Tyngd.App.Presentation;

public sealed partial class AdminPage : Page
{
    private bool _reloadWhenShown;

    public AdminPage()
    {
        this.InitializeComponent();
        this.GoBackOnBackButton();
        this.KeepFocusedInputVisible();
        DataContextChanged += (_, _) =>
        {
            ViewModel = DataContext as AdminViewModel;
            Bindings.Update();
            _ = ViewModel?.LoadAsync();
        };
        // Tillbaka från mallredigeraren: ladda om så att sparade mallar syns.
        Unloaded += (_, _) => _reloadWhenShown = true;
        Loaded += (_, _) =>
        {
            if (!_reloadWhenShown)
                return;
            _reloadWhenShown = false;
            _ = ViewModel?.LoadAsync();
        };
    }

    public AdminViewModel? ViewModel { get; private set; }

    private void OnUserSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            ViewModel?.SearchUsersCommand.Execute(null);
    }

    private async void OnAdminToggled(object sender, RoutedEventArgs e)
    {
        // Lika med det sparade värdet: bindningen satte det, eller så återställdes det nedan.
        if (ViewModel is null || sender is not ToggleSwitch { DataContext: AdminUserRow user } toggle || toggle.IsOn == user.IsAdmin)
            return;

        var isAdmin = toggle.IsOn;
        var action = isAdmin ? "ge adminbehörighet till" : "ta bort adminbehörighet från";
        var dialog = new ContentDialog
        {
            Title = "Bekräfta rolländring",
            Content = $"Vill du {action} {user.UserName}?",
            PrimaryButtonText = "Ja",
            CloseButtonText = "Avbryt",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowOverAsync(this) != ContentDialogResult.Primary || !await ViewModel.SetAdminRoleAsync(user, isAdmin))
            toggle.IsOn = user.IsAdmin;
    }

    private void OnEditTemplateClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AdminTemplateRow row)
            _ = ViewModel?.EditTemplateAsync(row);
    }

    private async void OnToggleTemplateClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || (sender as FrameworkElement)?.DataContext is not AdminTemplateRow row)
            return;

        var verb = row.Template.IsVisible ? "arkivera" : "återställa";
        var dialog = new ContentDialog
        {
            Title = "Bekräfta",
            Content = $"Vill du {verb} mallen {row.Name}?",
            PrimaryButtonText = "Ja",
            CloseButtonText = "Avbryt",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowOverAsync(this) == ContentDialogResult.Primary)
            await ViewModel.ToggleTemplateVisibilityAsync(row);
    }
}
