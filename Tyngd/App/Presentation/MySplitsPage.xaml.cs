namespace Tyngd.App.Presentation;

public sealed partial class MySplitsPage : Page
{
    public MySplitsPage()
    {
        this.InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            ViewModel = DataContext as MySplitsViewModel;
            Bindings.Update();
            _ = ViewModel?.LoadAsync();
        };
        Loaded += (_, _) => _ = ViewModel?.LoadAsync();
        // Fliksidan behålls mellan flikbyten, så ladda om när den visas igen (t.ex. efter EditSplit).
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible)
                _ = ViewModel?.LoadAsync();
            else if (ViewModel is not null)
                ViewModel.SuccessMessage = null; // Meddelandet gäller bara tills man lämnar fliken.
        });
    }

    public MySplitsViewModel? ViewModel { get; private set; }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || (sender as FrameworkElement)?.DataContext is not SplitItem split)
            return;

        var dialog = new ContentDialog
        {
            Title = "Arkivera split",
            Content = "Splitten döljs från nya träningspass, men träningshistoriken bevaras. Vill du fortsätta?",
            PrimaryButtonText = "Arkivera",
            CloseButtonText = "Avbryt",
            DefaultButton = ContentDialogButton.Close,
        };

        if (await dialog.ShowOverAsync(this) == ContentDialogResult.Primary)
            await ViewModel.DeleteAsync(split);
    }
}
