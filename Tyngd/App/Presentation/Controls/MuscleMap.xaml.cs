using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Tyngd.Shared.Catalog;

namespace Tyngd.App.Presentation.Controls;

/// <summary>Kroppsfigur som visar belastade muskler (som MuscleMap.razor i webbappen). Tryck eller använd knappen för att vända.</summary>
public sealed partial class MuscleMap : UserControl
{
    private const int MaxLegend = 6;

    /// <summary>Belastning per muskelnyckel (se MuscleLoad). Skalas mot det högsta värdet.</summary>
    public static readonly DependencyProperty MuscleLoadProperty = DependencyProperty.Register(
        nameof(MuscleLoad), typeof(IReadOnlyDictionary<string, double>), typeof(MuscleMap),
        new PropertyMetadata(null, (d, _) => ((MuscleMap)d).Update()));

    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
        nameof(EmptyText), typeof(string), typeof(MuscleMap),
        new PropertyMetadata("Inga muskeldata. Övningar måste vara kopplade till katalogen."));

    private bool _showBack;

    public MuscleMap()
    {
        this.InitializeComponent();
        Update();
    }

    public IReadOnlyDictionary<string, double>? MuscleLoad
    {
        get => (IReadOnlyDictionary<string, double>?)GetValue(MuscleLoadProperty);
        set => SetValue(MuscleLoadProperty, value);
    }

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    private void Update()
    {
        var muscles = Tyngd.Shared.Catalog.MuscleLoad.Scale(MuscleLoad);

        EmptyLabel.Visibility = muscles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        MapRoot.Visibility = muscles.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        foreach (var muscle in Muscles.All)
        {
            var main = (Path)FindName($"Main{muscle.WgerId}");
            var secondary = (Path)FindName($"Secondary{muscle.WgerId}");
            var shown = muscles.FirstOrDefault(item => item.Muscle == muscle);
            // Stark belastning ritas röd, lägre orange (wgers två bilder); wgers lager har själva opacitet 0,524.
            main.Visibility = shown is { IsStrong: true } ? Visibility.Visible : Visibility.Collapsed;
            secondary.Visibility = shown is { IsStrong: false } ? Visibility.Visible : Visibility.Collapsed;
            main.Opacity = 0.524;
            secondary.Opacity = 0.524 * (shown?.Opacity ?? 1);
        }

        Legend.ItemsSource = muscles.Take(MaxLegend)
            .Select(item => new LegendItem(item.Muscle.NameSv, item.IsStrong ? 1 : 0))
            .ToList();

        _showBack = Tyngd.Shared.Catalog.MuscleLoad.ShowBackFirst(muscles);
        ShowSide();
    }

    private void ShowSide()
    {
        FrontSide.Visibility = _showBack ? Visibility.Collapsed : Visibility.Visible;
        BackSide.Visibility = _showBack ? Visibility.Visible : Visibility.Collapsed;
        FlipLabel.Text = _showBack ? "Visa framsidan" : "Visa baksidan";
        AutomationProperties.SetName(Figure, _showBack
            ? "Visar baksidan. Tryck för att visa framsidan."
            : "Visar framsidan. Tryck för att visa baksidan.");
    }

    private void Flip()
    {
        _showBack = !_showBack;
        ShowSide();
    }

    private void OnFigureTapped(object sender, TappedRoutedEventArgs e) => Flip();

    private void OnFlipClick(object sender, RoutedEventArgs e) => Flip();

    public sealed record LegendItem(string Name, double StrongOpacity);
}
