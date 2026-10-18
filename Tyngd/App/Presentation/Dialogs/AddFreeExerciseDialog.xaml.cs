using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation.Dialogs;

/// <summary>Väljer en övning att lägga till i ett löst pass, som AddFreeExerciseDialog i webbappen.</summary>
public sealed partial class AddFreeExerciseDialog : ContentDialog
{
    public AddFreeExerciseDialog(CatalogService catalog, FavoritesService favorites, IReadOnlySet<int> taken)
    {
        this.InitializeComponent();
        Picker.Taken = taken;
        _ = Picker.LoadAsync(catalog, favorites);
    }

    /// <summary>Den valda övningen, eller null om dialogen avbröts.</summary>
    public FreeSessionExerciseDto? Picked { get; private set; }

    private void OnExercisePicked(object? sender, FreeSessionExerciseDto exercise)
    {
        Picked = exercise;
        Hide();
    }
}
