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
        var max = MuscleLoad?.Values.DefaultIfEmpty(0).Max() ?? 0;
        var muscles = max <= 0
            ? new()
            : MuscleLoad!
                .Select(pair => (Muscle: Muscles.Find(pair.Key), Share: pair.Value / max))
                .Where(item => item.Muscle is not null && item.Share > 0)
                .Select(item => (Muscle: item.Muscle!, item.Share))
                .OrderByDescending(item => item.Share)
                .ToList();

        EmptyLabel.Visibility = muscles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        MapRoot.Visibility = muscles.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        foreach (var muscle in Muscles.All)
        {
            var main = (Path)FindName($"Main{muscle.WgerId}");
            var secondary = (Path)FindName($"Secondary{muscle.WgerId}");
            var share = muscles.FirstOrDefault(item => item.Muscle == muscle).Share;
            // Stark belastning ritas röd, lägre orange (wgers två bilder); wgers lager har själva opacitet 0,524.
            main.Visibility = share >= 0.75 ? Visibility.Visible : Visibility.Collapsed;
            secondary.Visibility = share > 0 && share < 0.75 ? Visibility.Visible : Visibility.Collapsed;
            main.Opacity = 0.524;
            secondary.Opacity = 0.524 * (0.45 + 0.55 * share / 0.75);
        }

        Legend.ItemsSource = muscles.Take(MaxLegend)
            .Select(item => new LegendItem(item.Muscle.NameSv, item.Share >= 0.75 ? 1 : 0))
            .ToList();

        // Börja på den sida där det mesta händer.
        _showBack = muscles.Where(m => !m.Muscle.IsFront).Sum(m => m.Share) > muscles.Where(m => m.Muscle.IsFront).Sum(m => m.Share);
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
