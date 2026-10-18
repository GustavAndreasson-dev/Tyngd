using Tyngd.Shared.DTOs;
using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.App.Presentation.Dialogs;

/// <summary>Spara ett avslutat löst pass som ett riktigt pass, eller släng det (FinishFreeSessionDialog i webbappen).</summary>
public sealed partial class FinishFreeSessionDialog : ContentDialog
{
    private readonly FreeSessionService _freeSessions;
    private readonly int _passId;
    private readonly TrainingSplit _newSplit = new() { SplitId = 0, SplitName = "Ny split" };
    private bool _done;

    public FinishFreeSessionDialog(FreeSessionService freeSessions, UserSplitsService userSplits, int passId, string passName)
    {
        _freeSessions = freeSessions;
        _passId = passId;
        this.InitializeComponent();
        PassNameBox.Text = passName;
        TargetBox.ItemsSource = new List<TrainingSplit> { _newSplit };
        TargetBox.SelectedIndex = 0;
        _ = LoadSplitsAsync(userSplits);
    }

    /// <summary>Det sparade passet, eller null om passet slängdes.</summary>
    public StartFreeSessionResponse? Kept { get; private set; }

    private async Task LoadSplitsAsync(UserSplitsService userSplits)
    {
        try
        {
            TargetBox.ItemsSource = new List<TrainingSplit> { _newSplit }.Concat(await userSplits.GetMySplitsAsync()).ToList();
            TargetBox.SelectedIndex = 0;
        }
        catch (HttpRequestException) { }
    }

    private bool IsNewSplit => TargetBox.SelectedItem is not TrainingSplit { SplitId: > 0 };

    private void OnTargetChanged(object sender, SelectionChangedEventArgs e) =>
        NewSplitNameBox.Visibility = IsNewSplit ? Visibility.Visible : Visibility.Collapsed;

    private async void OnKeepClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        IsPrimaryButtonEnabled = IsSecondaryButtonEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            Kept = await _freeSessions.KeepAsync(_passId, new KeepFreeSessionRequest
            {
                PassName = PassNameBox.Text,
                TargetSplitId = IsNewSplit ? null : ((TrainingSplit)TargetBox.SelectedItem).SplitId,
                NewSplitName = IsNewSplit ? NewSplitNameBox.Text : null,
            });
            _done = true;
        }
        catch (HttpRequestException)
        {
            ErrorText.Text = "Kunde inte spara. Kontrollera namnen (1–50 tecken) och anslutningen.";
            args.Cancel = true;
        }
        finally
        {
            IsPrimaryButtonEnabled = IsSecondaryButtonEnabled = true;
            deferral.Complete();
        }
    }

    private async void OnDiscardClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        IsPrimaryButtonEnabled = IsSecondaryButtonEnabled = false;
        try
        {
            await _freeSessions.DiscardAsync(_passId);
        }
        catch (HttpRequestException)
        {
            // Passet syns ändå inte i några listor; det som tränats är sparat.
        }
        _done = true;
        deferral.Complete();
    }

    // Som BackdropClick/CloseOnEscapeKey = false i webbappen: bara knapparna stänger.
    private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args) => args.Cancel = !_done;
}
