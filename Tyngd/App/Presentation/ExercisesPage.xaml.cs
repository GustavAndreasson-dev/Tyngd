namespace Tyngd.App.Presentation;

public sealed partial class ExercisesPage : Page
{
    public ExercisesPage()
    {
        this.InitializeComponent();
        this.KeepFocusedInputVisible();
        Loaded += (_, _) =>
        {
            ViewModel?.Attach();
            Refresh();
        };
        Unloaded += (_, _) => ViewModel?.Detach();
        DataContextChanged += (_, _) =>
        {
            if (IsLoaded)
                ViewModel?.Attach();
            Refresh();
        };
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible)
                Refresh();
        });
    }

    private ExercisesViewModel? ViewModel => DataContext as ExercisesViewModel;

    // Katalogen hämtas bara en gång; stjärnorna synkas varje gång (favoriter kan ha ändrats på en annan sida).
    private async void Refresh()
    {
        if (ViewModel is not { } viewModel)
            return;
        await viewModel.LoadAsync();
        viewModel.SyncFavorites();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e) => ViewModel?.OpenCommand.Execute(e.ClickedItem);
}
