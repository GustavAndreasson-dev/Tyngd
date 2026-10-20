using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation.Dialogs;

/// <summary>Startar ett löst pass med en första övning, som StartFreeSessionDialog i webbappen. Sidan navigerar sedan till passet.</summary>
public sealed partial class StartFreeSessionDialog : ContentDialog
{
    private readonly FreeSessionService _freeSessions;

    public StartFreeSessionDialog(FreeSessionService freeSessions, CatalogService catalog, FavoritesService favorites)
    {
        _freeSessions = freeSessions;
        this.InitializeComponent();
        _ = Picker.LoadAsync(catalog, favorites);
    }

    /// <summary>Det startade passet, eller null om dialogen avbröts.</summary>
    public StartFreeSessionResponse? Started { get; private set; }

    private async void OnExercisePicked(object? sender, FreeSessionExerciseDto first)
    {
        Progress.Visibility = Visibility.Visible;
        Picker.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;
        try
        {
            Started = await _freeSessions.StartAsync(new StartFreeSessionRequest
            {
                PassName = FreeSessionService.DefaultPassName(DateTime.Now),
                Exercises = [first],
            });
            Hide();
        }
        catch (HttpRequestException)
        {
            ErrorText.Text = "Kunde inte starta passet. Kontrollera anslutningen.";
            Progress.Visibility = Visibility.Collapsed;
            Picker.Visibility = Visibility.Visible;
        }
    }
}
