using Windows.Foundation;

namespace Tyngd.App.Presentation;

public static class DialogExtensions
{
    /// <summary>
    /// Visar dialogen med sidans tema. Dialogens ram följer annars telefonens tema (Uno), så en mörk telefon gav en
    /// mörk dialog när appen var i ljust läge. Dialogens färger hämtas därför ur sidans temaordbok.
    /// </summary>
    public static IAsyncOperation<ContentDialogResult> ShowOverAsync(this ContentDialog dialog, FrameworkElement owner)
    {
        dialog.XamlRoot = owner.XamlRoot;
        dialog.RequestedTheme = owner.ActualTheme;
        var theme = owner.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        foreach (var (dialogKey, paletteKey) in ThemedKeys)
            if (FindThemeResource(Application.Current.Resources, theme, paletteKey) is { } value)
                dialog.Resources[dialogKey] = value;
        if (dialog.Resources.TryGetValue("MaterialContentDialogBackground", out var background) && background is Brush brush)
            dialog.Background = brush;
        // Mallen läser inte titelfärgen från dialogens resurser; ge en texttitel färgen direkt.
        if (dialog.Title is string title && dialog.Resources.TryGetValue("MaterialContentDialogTitleForeground", out var titleBrush))
            dialog.Title = new TextBlock
            {
                Text = title,
                Style = (Style)Application.Current.Resources["HeadlineSmall"],
                Foreground = (Brush)titleBrush,
                TextWrapping = TextWrapping.Wrap,
            };
        return dialog.ShowAsync();
    }

    // Materials dialogresurser (som annars läses i telefonens tema) och paletten de ska ha.
    private static readonly (string DialogKey, string PaletteKey)[] ThemedKeys =
    [
        ("MaterialContentDialogBackground", "SurfaceBrush"),
        ("MaterialContentDialogTitleForeground", "OnSurfaceBrush"),
        ("MaterialContentDialogContentForeground", "OnSurfaceVariantBrush"),
    ];

    private static object? FindThemeResource(ResourceDictionary dictionary, string theme, string key)
    {
        if (dictionary.ThemeDictionaries.TryGetValue(theme, out var themed)
            && themed is ResourceDictionary themeDictionary
            && themeDictionary.TryGetValue(key, out var value))
            return value;

        foreach (var merged in dictionary.MergedDictionaries)
            if (FindThemeResource(merged, theme, key) is { } found)
                return found;

        return null;
    }
}
